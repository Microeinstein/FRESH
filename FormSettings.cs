using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Micro.WinForms;

namespace FRESH {
    partial class FormSettings : Form, IKeepFormOpen {
        const string MSG_BASE    = "There are some unsaved changes. ",
                     MSG_SAVE    = "Do you want to save them?",
                     MSG_DISCARD = "Are you sure?";
        bool realClose = false,
             loading   = false,
             changed   = false;
        static readonly Size[] preferred = new[] {
            new Size(1024, 300),
            new Size(580, 300),
        };

        //TODO: Implement entirely new feeds editor GUI with same functions.
        public FormSettings() {
            InitializeComponent();
            txtSound.Text = Program.SYSTEM_SND_FEED;
            editor.ApplyIcons(Program.icons);
            editor.listChanged           += editor_listChanged;
            txtSound.TextChanged         += changeEvent;
            numMax.ValueChanged          += changeEvent;
            chkDisableNtf.CheckedChanged += changeEvent;
            chkDisableSnd.CheckedChanged += changeEvent;
            load(false);
        }
        void form_formClosing(object sender, FormClosingEventArgs e) {
            e.Cancel = !realClose;
            if (!realClose)
                Hide();
        }
        void btnOK_Click(object sender, EventArgs e) {
            save();
            Close();
        }
        void btnCancel_Click(object sender, EventArgs e) {
            if (load(true, Hide))
                Close();
        }
        void btnReload_Click(object sender, EventArgs e) {
            load(true, Program.worker.load);
        }
        void btnApply_Click(object sender, EventArgs e) {
            save();
        }
        void tabs_SelectedIndexChanged(object sender, EventArgs e)
            => updateSize(tabs.SelectedIndex);
        void editor_listChanged() {
            changeEvent(null, null);
            updateSize(0);
        }
        
        public void CloseForReal() {
            realClose = true;
            Close();
        }
        public bool CheckUnsaved(bool discard) { //true => proceed
            if (!changed)
                return true;
            var r = MessageBox.Show(
                MSG_BASE + (discard ? MSG_DISCARD : MSG_SAVE),
                Program.NAME,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning
            );
            if (r == DialogResult.Yes) {
                if (!discard)
                    save();
                return true;
            } else if (r == DialogResult.No) {
                if (!discard)
                    return true;
            }
            return false;
        }
        internal bool load(bool discard, Action beforeLoad = null) { //true => proceed
            if (!CheckUnsaved(discard))
                return false;
            changed = false;
            beforeLoad?.Invoke();
            if (Visible)
                this.SuspendDrawing();
            loading = true;
            editor.load();
            txtSound.Text         = Settings.options.defaultSoundEffectPath.cast<string>();
            numMax.Value          = Settings.options.maxElementsPerFeed.cast<int>();
            chkDisableNtf.Checked = Settings.options.disableNotification.cast<bool>();
            chkDisableSnd.Checked = Settings.options.disableSound.cast<bool>();
            loading = false;
            if (Visible)
                this.ResumeDrawing();
            return true;
        }
        void save() {
            editor.save();
            Settings.options.defaultSoundEffectPath.value = txtSound.Text;
            Settings.options.maxElementsPerFeed.value     = numMax.Value;
            Settings.options.disableNotification.value    = chkDisableNtf.Checked;
            Settings.options.disableSound.value           = chkDisableSnd.Checked;
            if (!Program.worker.closing)
                Program.worker.save();
            unsavedChangesState(changed = false);
            if (!Program.worker.closing)
                Program.worker.refresh();
        }
        void changeEvent(object sender, EventArgs e) {
            if (!loading && !changed)
                unsavedChangesState(changed = true);
        }
        void unsavedChangesState(bool v) {
            this.Text = Program.NAME + (v ? "*" : "");
        }
        void updateSize(int tab) {
            Size prefS = preferred[tab],
                 currS = this.Size;
            Rectangle workArea = Screen.GetWorkingArea(this);
            Point currP = this.Location;
            if (tab == 0)
                prefS.Height = Math.Min(
                    Math.Max(prefS.Height, editor.SuggestedHeight + 132),
                    workArea.Height
                );
            //prefP = new Point(
            //    currP.X + (currS.Width / 2) - (prefS.Width / 2),
            //    currP.Y + (currS.Height / 2) - (prefS.Height / 2)
            //);
            Size = MinimumSize = prefS;
            int yDiff = Bounds.Bottom - workArea.Height;
            if (yDiff > 0) {
                currP.Y -= yDiff;
                this.Location = currP;
            }
            //Location = prefP;
        }
    }
}
