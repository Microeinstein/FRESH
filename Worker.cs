#define NO_PARALLEL
#undef NO_PARALLEL
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Micro.WinForms;
using static FRESH.Program;

namespace FRESH {
    using Timer = Micro.Threading.Timer;
    using FeedEntry = SyndicationItem;
    using FeedTSMs = List<FeedTSM>;
    using FeedNewEntries = List<SyndicationItem>;
    using FeedChangedEntries = Dictionary<string, SyndicationItem>;

    class Worker : ComponentContext {
        const int HANDLE_NOT_CREATED = unchecked((int)0x80131509);
        internal static readonly SoundPlayer sysSndFeed;
        static readonly MouseEventArgs
            LEFT_CLICK = new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0);
        NotifyIconEx notifier;
        ContextMenuStrip menu;
        ToolStripItem[] fixedItems;
        ToolStripMenuItem itemStartup, itemExit;
        FormSettings settingsForm;
        FeedTSMs menuFeeds;
        Action BalloonClick;
        Timer clock;
        int maxEntries => Settings.options.maxElementsPerFeed.cast<int>();
        internal bool closing = false;

        FeedTSM this[Feed f]
            => menuFeeds.FirstOrDefault(i => i.feed == f);

        static Worker() {
            try {
                sysSndFeed = new SoundPlayer(Environment.ExpandEnvironmentVariables(Program.SYSTEM_SND_FEED));
                sysSndFeed.LoadAsync();
            } catch (Exception) { }
        }
        public Worker() : base(new NotifyIconEx()) {
            InitializeMyComponent();
            load();
            settingsForm.load(false);
            refresh();
        }
        ToolStripItem[] MenuAdd(params (string text, object img, Action click)?[] items) {
            var tsi = new ToolStripItem[items.Length];
            int i = 0;
            foreach (var tuple in items) {
                if (tuple == null)
                    menu.Items.Add(tsi[i++] = new ToolStripSeparator());
                else {
                    var clk = tuple.Value.click;
                    var pic = tuple.Value.img;
                    tsi[i++] = menu.Items.Add(
                        tuple.Value.text,
                        pic is Icon ico
                            ? ico.ToBitmap()
                            : pic is Image img
                                ? img
                                : null,
                        (a, b) => clk?.Invoke()
                    );
                }
            }
            return tsi;
        }
        void InitializeMyComponent() {
            notifier = (NotifyIconEx)Component;
            notifier.Text = Program.NAME;
            notifier.Icon = Properties.Resources.newsfeed;
            menu = new ContextMenuStrip() {
                Name = "Menu",
                Size = new Size(61, 4),
                ShowItemToolTips = true,
            };
            void closeClick() {
                close();
                Application.Exit();
            }
            fixedItems = MenuAdd(
                null,
                ("Refresh",           Properties.Resources.arrow_circle_315, refresh),
                null,
                ("Settings",          Properties.Resources.gear__pencil,     showSettings),
                ("Launch at startup", Properties.Resources.control_power,    toggleStartup),
                ("Exit",              Properties.Resources.cross,            closeClick)
            );
            menu.CreateControl();
            menu.Show(-9999, -9999); //Absolutely required for handle creation
            menu.Close();
            settingsForm = new FormSettings();
            menuFeeds    = new FeedTSMs();
            itemStartup  = (ToolStripMenuItem)fixedItems[4];
            itemExit     = (ToolStripMenuItem)fixedItems[5];
            clock        = new Timer(1000 * 60, clockCheck, "Background timer");
            notifier.Wrapped.MouseUp += (a, b) => atMenuOpen();
            notifier.Wrapped.BalloonTipClicked += notifierBalloonClick;
            notifier.LeftClickMenu =
            notifier.RightClickMenu = menu;
            notifier.Visible = true;
        }

        void atMenuOpen() {
            itemStartup.Checked = GetStartup(false);
            restoreDefaultIcon();
        }
        void showSettings() {
            settingsForm.Show();
        }
        void toggleStartup() {
            try {
                ToggleStartup();
            } catch (Exception ex) {
                showException("Unable to toggle startup", ex);
            }
        }
        protected override void close() {
            if (closing)
                return;
            closing = true;
            if (!settingsForm.CheckUnsaved(false))
                return;
            clock.Stop();
            notifier.Visible = false;
            settingsForm.CloseForReal();
            save();
            foreach (var feed in Settings.feeds)
                feed.Dispose();
        }

        internal void load() {
            Settings.LoadFeeds();
            Settings.options.Load();
            loadMenu();
        }
        internal void save() {
            Settings.SaveFeeds();
            Settings.options.Save();
            if (!closing)
                loadMenu();
        }
        internal void refresh() {
            clock.Stop();
            Task.Run(async () => {
                Debug.WriteLine("Checking in 5 seconds...");
                await Task.Delay(5000);
                Debug.WriteLine("Checking...");
                checkFeeds(true);
                Invoke<Action>(() => clock.Start());
            });
        }
        void refreshSingle(FeedTSM f) {
            clock.Stop();
            Task.Run(() => {
                checkFeed(Program.FastNow, f, true);
                SystemSounds.Asterisk.Play();
                Invoke<Action>(() => clock.Start());
            });
        }
        void loadMenu() {
            void init(FeedTSM item) {
                if (item.feed.Content == null)
                    return;
                var contents = item.feed.Content.Items;
                int max = maxEntries;
                if (max > 0)
                    contents = contents.Take(max);
                contents = contents.Reverse();
                foreach (var feed in contents) {
                    var fet = new FeedEntryTSM(item, feed, feedItemClick);
                    fet.Unread = false;
                    item.DropDownItems.Add(fet);
                }
            }

            foreach (var item in menuFeeds)
                menu.Items.Remove(item);

            var ff = Settings.feeds;

            var rem = menuFeeds.Where(m => !ff.Contains(m.feed)).ToArray();
            foreach (var item in rem) {
                menuFeeds.Remove(item);
                item.Dispose();
            }

            foreach (var f in ff) {
                if (this[f] != null)
                    continue;
                var m = new FeedTSM(f, feedClick) { Unread = false };
                init(m);
                menuFeeds.Add(m);
            }

            void swap(int a, int b) {
                var tmp = menuFeeds[a];
                menuFeeds[a] = menuFeeds[b];
                menuFeeds[b] = tmp;
            }
            for (int b, a = 0; a < ff.Count; a++) {
                b = menuFeeds.FindIndex(m => m.feed == ff[a]);
                if (b != a)
                    swap(a, b);
            }

            foreach (var item in ((IEnumerable<FeedTSM>)menuFeeds).Reverse())
                menu.Items.Insert(0, item);
        }
        void clockCheck() {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            checkFeeds(false);
        }
        void checkFeeds(bool force = false) {
            var now = FastNow;
            
            bool ch = false;
            Task<bool> chkParallel(FeedTSM f)
                => Task.Run(() => ch |= checkFeed(now, f, force));

            lock (Settings.options) {
                lock (Settings.feeds) {
#if NO_PARALLEL
                    foreach (var f in menuFeeds)
                        ch |= checkFeed(now, f, force);
#else
                    var tasks = menuFeeds.Select(f => chkParallel(f)).ToArray();
                    Task.WaitAll(tasks);
#endif
                    Settings.options.lastFeedCheck.value = now;
                    if (ch)
                        Invoke<Action>(notifyNewContent);
                }
            }
        }
        bool checkFeed(DateTime now, FeedTSM f, bool force = false) {
            if (closing)
                return false;
            if (f.feed.Checking || (!force && !f.feed.IsTime(now)))
                return false;
            try {
                if (f.feed.Check(out var newi, out var changedi, force)) {
                    f.lastError = null;
                    f.lastNewItems = newi;
                    f.lastChangedItems = changedi;
                } else
                    return false;
            } catch (Exception ex) {
                f.lastError = ex;
                f.lastNewItems = null;
                f.lastChangedItems = null;
            }
            Invoke<Action<FeedTSM>>(updateMenuItem, f);
            return true;
        }

        void showException(string title, Exception ex) {
            notifier.BalloonTipIcon = ToolTipIcon.Warning;
            notifier.BalloonTipTitle = title;
            notifier.BalloonTipText = ex?.Message ?? "";
        }
        void updateMenuItem(FeedTSM item) {
            if (item.lastError != null) {
                item.Image = Properties.Resources.exclamation;
                return;
            }
            item.Image = item.feed.Logo;
            if (item.lastNewItems.Count == 0 && item.lastChangedItems.Count == 0)
                return;
            item.Unread = true;
            foreach (var kv in item.lastChangedItems) {
                var fimi = (FeedEntryTSM)item.DropDownItems[kv.Key];
                fimi.item = kv.Value;
                fimi.Unread = true;
            }
            foreach (var fi in ((IEnumerable<FeedEntry>)item.lastNewItems).Reverse())
                item.DropDownItems.Add(new FeedEntryTSM(item, fi, feedItemClick));
            int max = maxEntries,
                count = item.DropDownItems.Count;
            if (max > 0 && count > max) {
                int to = count - max - 1;
                for (int i = 0; i < to; i++)
                    item.DropDownItems.RemoveAt(0);
            }
        }
        void notifyNewContent() {
            int changedFeeds = 0,
                totalNew = 0,
                totalChanges = 0;
            FeedTSM lastCheckedTSM = null;
            FeedEntry lastNew = null;
            FeedEntryTSM lastChanged = null;

            foreach (var menufeed in menuFeeds) {
                if (menufeed.lastError != null)
                    continue;
                var ln = menufeed.lastNewItems;
                var lc = menufeed.lastChangedItems;
                if (ln?.Count > 0 || lc?.Count > 0) {
                    lastCheckedTSM = menufeed;
                    changedFeeds++;
                    int l;

                    l = ln.Count;
                    if (l > 0)
                        lastNew = ln[0];
                    totalNew += l;

                    l = lc.Count;
                    if (l > 0)
                        lastChanged = (FeedEntryTSM)menufeed.DropDownItems[lc.First().Value.Id];
                    totalChanges += l;
                }
            }
            if (changedFeeds == 0)
                return;
            var n = notifier.Wrapped;
            n.BalloonTipIcon = ToolTipIcon.Info;
            var feed = lastCheckedTSM?.feed;
            if (changedFeeds == 1) {
                if (totalNew == 1 && totalChanges == 0) {
                    //Program.SetTitle(n, feed.Logo, feed.name);
                    n.BalloonTipTitle = feed.name;
                    n.BalloonTipText = lastNew.Title.Text;
                    BalloonClick += () => execute(feed, lastNew);
                    goto popup;
                } else if (totalNew == 0 && totalChanges == 1) {
                    //Program.SetTitle(n, feed.Logo, feed.name);
                    n.BalloonTipTitle = feed.name;
                    n.BalloonTipText = "An entry has changed.";
                    BalloonClick += () => select(lastChanged);
                    goto popup;
                }
            }
            //Program.SetTitle(n, null, Program.NAME);
            n.BalloonTipTitle = Program.NAME;
            var sb = new StringBuilder("There are ");
            if (totalNew > 0)     sb.Append($"{totalNew} new entries, ");
            if (totalChanges > 0) sb.Append($"{totalChanges} changed entries, ");
            sb.Append($"from {changedFeeds} feeds.");
            n.BalloonTipText = sb.ToString();
            BalloonClick += () => notifier.Click(LEFT_CLICK);

            popup:
            notifier.Icon = Properties.Resources.newsbell;
            if (!Settings.options.disableNotification.cast<bool>())
                notifier.Wrapped.ShowBalloonTip(30);
            soundNotify(changedFeeds == 1 ? lastCheckedTSM : null);
        }
        void soundNotify(FeedTSM f) {
            if (Settings.options.disableSound.cast<bool>())
                return;
            try {
                if (f == null) {
                    sysSndFeed?.Play();
                    return;
                }
                //TODO: Implement custom sounds with NAudio library.
            } catch (Exception) { }
        }

        void feedClick(object sender, MouseEventArgs click) {
            var item = (FeedTSM)sender;
            switch (click.Button) {
                case MouseButtons.Left:
                    refreshSingle(item);
                    break;
                case MouseButtons.Right:
                    if (item.lastError != null) {
                        MessageBox.Show(
                            "Error updating the feed: \n" + item.lastError.ToString(),
                            Program.NAME,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation
                        );
                        return;
                    }
                    var desc = item.feed.Content?.Description?.Text;
                    if (desc == null)
                        return;
                    menu.Close();
                    MessageBox.Show(
                        desc, $"{Program.NAME} - {item.feed.name}",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    break;
                case MouseButtons.Middle:
                    break;
            }
        }
        void feedItemClick(object sender, MouseEventArgs click) {
            var item = (FeedEntryTSM)sender;
            bool shift = Control.ModifierKeys.HasFlag(Keys.Shift);
            switch (click.Button) {
                case MouseButtons.Left:
                    if (shift)
                        openLink(item.item);
                    else
                        execute(item.parent.feed, item.item);
                    break;
                case MouseButtons.Right:
                    if (shift) {
                        if (item.item.Links?.ElementAt(0)?.Uri?.ToString() is string uri) {
                            string clip = null;
                            if (item.parent.feed.name is string name)
                                clip = name;
                            if (item.item.Title?.Text is string title) {
                                if (clip != null)
                                    clip += " ~ " + title;
                                else
                                    clip = title;
                            }
                            clip += ": " + uri;
                            Clipboard.SetText(clip);
                            SystemSounds.Asterisk.Play();
                        }
                        return;
                    }
                    var desc = item.item.Summary?.Text;
                    if (desc == null)
                        return;
                    MessageBox.Show(
                        desc, $"{Program.NAME} - {item.parent.feed.name}/{item.item.Title?.Text}",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
            }
        }
        void notifierBalloonClick(object sender, EventArgs e) {
            BalloonClick?.Invoke();
            BalloonClick = null;
        }
        void select(ToolStripMenuItem item) {
            if (item == null)
                return;
            var parent = (ToolStripMenuItem)item.OwnerItem;
            if (parent != null) {
                select(parent);
                parent.ShowDropDown();
            } else
                notifier.Click(LEFT_CLICK);
            item.Select();
        }
        void execute(Feed feed, FeedEntry item) {
            try {
                string cmd = feed.FormatOpenAction(item);
                var parts = Program.CommandToExeArgs(cmd, false);
                Process.Start(parts[0], parts[1]);
            } catch (Exception ex) {
                showException("Launch error", ex);
            }
        }
        void openLink(FeedEntry item) {
            try {
                //TODO: Add new field "middle click action" (now it launches the first link of the entry).
                //TODO: (Idea) Allow entire personalization of mouse actions.
                string cmd = item.Links?.ElementAt(0)?.Uri?.ToString();
                Process.Start(cmd);
            } catch (Exception) {
                //showException("Launch error", ex);
            }
        }

        internal void restoreDefaultIcon() {
            if (notifier.Icon == Properties.Resources.newsfeed)
                return;
            if (menuFeeds.All(mf => mf.DropDownItems.OfType<FeedEntryTSM>().All(fe => !fe.Unread)))
                notifier.Icon = Properties.Resources.newsfeed;
        }

        void Invoke<T>(T method, params object[] args) where T : Delegate {
            do {
                try {
                    menu.Invoke(method, args);
                    Debug.WriteLine("Successful invocation.");
                    break;
                } catch (InvalidOperationException ex) {
                    if (ex.HResult == HANDLE_NOT_CREATED) {
                        Debug.WriteLine($"{nameof(HANDLE_NOT_CREATED)} exception.");
                        Task.Delay(2000).Wait();
                    } else
                        throw ex;
                }
            } while (true);
        }
    }

    class FeedTSM : ToolStripMenuItem, IDisposable {
        public readonly Feed feed;
        public Exception lastError = null;
        public FeedNewEntries lastNewItems = null;
        public FeedChangedEntries lastChangedItems = null;
        public bool Unread {
            get => _unread;
            set {
                if (value == _unread)
                    return;
                if (value) {
                    this.Font = new Font(this.Font, FontStyle.Bold);
                    //this.ToolTipText = feed.Content?.Description?.Text;
                } else {
                    this.Font = new Font(this.Font, FontStyle.Regular);
                }
                _unread = value;
            }
        }
        bool _unread = false;

        public FeedTSM(Feed f, MouseEventHandler mouseUp) {
            this.feed = f;
            this.Text = f.name;
            this.Image = f.Logo;
            this.AutoToolTip = false;
            this.MouseUp += mouseUp;
            this.MouseEnter += (a, b) => Unread = false;
            f.LogoRemoving += logoRemoving;
            f.LogoAssigned += logoAssigned;
            Unread = true;
        }
        protected override void Dispose(bool disposing) {
            feed.LogoRemoving -= logoRemoving;
            feed.LogoAssigned -= logoAssigned;
            base.Dispose(disposing);
        }
        void logoRemoving(object sender, EventArgs e)
            => this.Image = null;
        void logoAssigned(object sender, EventArgs e)
            => this.Image = feed.Logo;
    }

    class FeedEntryTSM : ToolStripMenuItem {
        public readonly FeedTSM parent;
        public FeedEntry item;
        public bool Unread {
            get => _unread;
            set {
                if (value == _unread)
                    return;
                if (value) {
                    this.Name = item.Id;
                    this.Text = item.Title.Text;
                    this.Font = new Font(this.Font, FontStyle.Bold);
                    //TODO: Alternative to ToolTip for item.PublishDate.
                    //var date = item.PublishDate.DateTime;
                    //if (date.Year >= 2000) //Not 1970 or 0001
                    //    this.ToolTipText = "Published: " + date.ToString();
                } else {
                    this.Font = new Font(this.Font, FontStyle.Regular);
                }
                _unread = value;
            }
        }
        bool _unread = false;

        public FeedEntryTSM(FeedTSM parent, FeedEntry i, MouseEventHandler mouseUp) {
            this.parent = parent;
            this.item = i;
            this.Image = Properties.Resources.document_text;
            this.Name = i.Id;
            this.AutoToolTip = false;
            this.MouseUp += mouseUp;
            this.MouseEnter += (a, b) => {
                Unread = false;
                //HACK: Passing the worker istance would be preferred
                Program.worker.restoreDefaultIcon();
            };
            Unread = true;
        }
    }
}
