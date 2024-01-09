using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Micro.WinForms;

namespace FRESH {
    using FeedEntry = SyndicationItem;

    public class Feed : IExposable, ICloneable, IDisposable {
        const int
            ID_LENGTH = 8,
            CACHE_SIZE = 30,
            LOGO_SIZE = 48;
        static readonly Regex
            rgxOpenFormat = new Regex(@"<([^<>]+?)>"),
            rgxArray      = new Regex(@"([^\[\]]+)\[([0-9]+)\]");
        static readonly LinkComparer LINK_CMP = new LinkComparer();

        public event EventHandler LogoRemoving, LogoAssigned;

        public int RealValuesCount => 8;
        public int UIValuesCount => 7;
        public IEnumerable<object> RealUIValues {
            get {
                yield return disable;
                yield return name;
                yield return address;
                yield return checkSchedule;
                yield return openAction;
                yield return overrideSound;
                yield return customSound;
            }
        }
        public SyndicationFeed Content { get; protected set; }

        public string ID {
            get => _id;
            protected set {
                if (value == _id)
                    return;
                _id = value;

                string old;

                old = _xmlPath;
                _xmlPath = Path.Combine(Program.CacheDir, _id + ".xml");
                if (File.Exists(old))
                    File.Move(old, _xmlPath);

                old = _logoPath;
                _logoPath = Path.Combine(Program.CacheDir, _id + ".bin");
                if (File.Exists(old))
                    File.Move(old, _logoPath);
            }
        }
        public bool Checking => _checking;
        public string name, address, customSound, openAction;
        public bool disable, overrideSound;
        public CronSchedule checkSchedule;
        public Image Logo { get; protected set; }
        MemoryStream logoData;
        Queue<DateTime> nextCachedChecks = new Queue<DateTime>(CACHE_SIZE);
        DateTime? nextCheck;
        string lastXml = "";
        string _id, _xmlPath, _logoPath;
        volatile bool _checking = false;

        public Feed() {
            ID = Program.RandomHex(ID_LENGTH);
            checkSchedule = new CronSchedule();
        }
        public Feed(Feed copy = null) {
            ID            = Program.RandomHex(ID_LENGTH);
            disable       = copy?.disable ?? false;
            name          = copy?.name    ?? "Feed";
            address       = copy?.address ?? "localhost";
            checkSchedule = (CronSchedule)copy?.checkSchedule?.Clone() ?? new CronSchedule();
            openAction    = copy?.openAction    ?? "cmd /k echo \"<feed.name>\" \"<feed.address>\" \"<item.title>\" \"<item.summary>\" \"<item.links[0]>\"";
            overrideSound = copy?.overrideSound ?? false;
            customSound   = copy?.customSound;
        }
        public object Clone()
            => new Feed(this);
        public IEnumerable<string> TextValues(bool forUI) {
            if (!forUI)
                yield return ID;
            yield return disable.BoolToString(forUI);
            yield return name;
            yield return address;
            yield return checkSchedule.Expression;
            yield return openAction;
            yield return overrideSound.BoolToString(forUI);
            yield return customSound;
        }
        public void ParseAndSet(string v, int index, bool fromUI) {
            if (fromUI && index >= 0)
                index++;
            switch (index) {
                case 0: ID            = v; break;
                case 1: disable       = v.StringToBool(fromUI); break;
                case 2: name          = v; break;
                case 3: address       = v; break;
                case 4: checkSchedule.TryChange(v); break;
                case 5: openAction    = v; break;
                case 6: overrideSound = v.StringToBool(fromUI); break;
                case 7: customSound   = v; break;
            }
        }
        public bool ShouldBeMasked(int index)
            => false;

        public static string ParseVariable(string var, Feed feed = null, FeedEntry entry = null) {
            var parts = var.Split('.');
            bool parseFeed() {
                if (feed == null || parts[0] != "feed")
                    return false;
                switch (parts[1]) {
                    case "name":    var = feed.name; break;
                    case "address": var = feed.address; break;
                    default: return false;
                }
                return true;
            }
            bool parseItem() {
                if (entry == null || parts[0] != "item")
                    return false;
                switch (parts[1]) {
                    case "title":   var = entry.Title?.Text;   break;
                    case "summary": var = entry.Summary?.Text; break;
                    default:
                        var arr = rgxArray.Match(parts[1]);
                        if (!arr.Success)
                            return false;
                        int i = int.Parse(arr.Groups[2].Value);
                        switch (arr.Groups[1].Value) {
                            case "links" when i < entry.Links?.Count:
                                var = entry.Links[i].Uri.ToString();
                                break;
                            default: return false;
                        }
                        break;
                }
                return true;
            }
            bool ok = parseFeed() || parseItem();
            return var;
        }
        public string FormatOpenAction(FeedEntry entry) {
            if (string.IsNullOrWhiteSpace(openAction))
                return openAction;
            return rgxOpenFormat.Replace(openAction, m => {
                var grp = m.Groups[1];
                if (grp.Length == 0)
                    return m.Value;
                return ParseVariable(grp.Value, this, entry);
            });
        }
        public bool IsTime(DateTime now) {
            var lastCheck = Settings.options.lastFeedCheck.cast<DateTime>();
            bool fastforward, ret = false;
            do {
                if (nextCachedChecks.Count == 0) {
                    var nxt = checkSchedule.NextTimes(lastCheck).Take(CACHE_SIZE).ToArray();
                    foreach (var time in nxt) {
                        nextCachedChecks.Enqueue(time);
                        lastCheck = time;
                    }
                }
                nextCheck = nextCheck ?? nextCachedChecks.Dequeue();
                if (fastforward = (now >= nextCheck)) {
                    ret = true;
                    nextCheck = nextCachedChecks.Dequeue();
                }
            } while (fastforward);
            return ret;
        }
        public bool Check(out List<FeedEntry> newItems, out Dictionary<string, FeedEntry> changedItems, bool force = false) {
            _checking = true;
            newItems = null;
            changedItems = null;

            Program.ReadFeed(address, out string xml, out var synd);
            if (!force && xml == lastXml)
                return _checking = false;

            newItems = new List<FeedEntry>(0);
            changedItems = new Dictionary<string, FeedEntry>(0);
            int itemIndex = 0;

            int? prevCount = Content?.Items?.Count();
            FeedEntry prev;
            void nextPrev()
                => prev = itemIndex < prevCount ? Content.Items.ElementAt(itemIndex++) : null;
            var sb = new StringBuilder();
            /*string getAtomXml(FeedEntry i) {
                sb.Clear();
                using (var xw = XmlWriter.Create(sb))
                    i.GetAtom10Formatter().WriteTo(xw);
                return sb.ToString();
            }*/
            bool hasChanged(FeedEntry a, FeedEntry b) {
                return a.Id            != b.Id
                    || a.Title?.Text   != b.Title?.Text
                    || a.Summary?.Text != b.Summary?.Text
                    || a.PublishDate   != b.PublishDate
                    || (a.Links != null && b.Links != null && !a.Links.SequenceEqual(b.Links, LINK_CMP));
            }

            bool isNew = true;
            nextPrev();
            foreach (var newItem in synd.Items) {
                if (Content == null) {
                    newItems.Add(newItem);
                    continue;
                }
                if (isNew) {
                    if (newItem.Id == prev.Id)
                        isNew = false;
                    else {
                        newItems.Add(newItem);
                        continue;
                    }
                }
                //if (newItem.Id != prev.Id || getAtomXml(newItem) != getAtomXml(prev))
                if (hasChanged(prev, newItem))
                    changedItems[prev.Id] = newItem;
                nextPrev();
            }
            lastXml = xml;
            Content = synd;
            UpdateLogo();
            SaveCache();
            _checking = false;
            return true;
        }
        public void UpdateLogo(Uri uri = null) {
            uri = uri ?? Content?.ImageUrl;
            string u = uri?.ToString();
            removeLogo();
            if (u == null) {
                try {
                    u = new Uri(address).GetLeftPart(UriPartial.Authority) + "/favicon.ico";
                } catch (Exception) {
                    return;
                }
            }

            using (var str = Program.DownloadData(u))
                assignLogo(str);
            //using (var memOut = new MemoryStream()) {
            //    if (ImagingHelper.ConvertToIcon(memIn, memOut))
            //        Logo = new Icon(memOut);
            //}
        }
        internal void LoadCache() {
            try {
                lastXml = !File.Exists(_xmlPath) ? "" : File.ReadAllText(_xmlPath);
                if (Content == null && !string.IsNullOrWhiteSpace(lastXml)) {
                    Program.ReadFeed(lastXml, out var nc);
                    Content = nc;
                }
            } catch {
                lastXml = "";
                Content = null;
            }
            removeLogo();
            if (File.Exists(_logoPath)) {
                using (var str = File.OpenRead(_logoPath))
                    assignLogo(str);
            }
        }
        internal void SaveCache() {
            File.WriteAllText(_xmlPath, lastXml);
            if (logoData != null) {
                using (var str = File.OpenWrite(_logoPath)) {
                    long pos = logoData.Position;
                    logoData.Position = 0;
                    logoData.CopyTo(str);
                    logoData.Position = pos;
                }
            }
        }
        void removeLogo() {
            LogoRemoving?.Invoke(this, null);
            Logo?.Dispose();
            logoData?.Close();
            logoData?.Dispose();
            logoData = null;
            Logo = null;
        }
        void assignLogo(Stream str) {
            //There are good reasons for this mess...
            using (var mem1 = new MemoryStream()) {
                str.CopyTo(mem1);
                var img = Image.FromStream(mem1);
                if (img.Width > LOGO_SIZE || img.Height > LOGO_SIZE) {
                    var img2 = Program.ResizeImage(img, LOGO_SIZE, LOGO_SIZE);
                    img.Dispose();
                    img = img2;
                }
                logoData = new MemoryStream();
                img.Save(logoData, ImageFormat.Png);
                Logo = Image.FromStream(logoData);
                img.Dispose();
            }
            LogoAssigned?.Invoke(this, null);
        }

        public void Dispose() {
            removeLogo();
        }
        public override string ToString()
            => $"{name}, {(Content == null ? "null" : "exists")}, {(Logo == null ? "null" : "exists")}";

        class LinkComparer : IEqualityComparer<SyndicationLink> {
            public bool Equals(SyndicationLink x, SyndicationLink y)
                => x?.Uri?.ToString() == y?.Uri?.ToString();
            public int GetHashCode(SyndicationLink obj)
                => obj.GetHashCode();
        }
    }
}
