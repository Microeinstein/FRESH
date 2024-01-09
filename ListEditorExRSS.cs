using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Micro.WinForms;

namespace FRESH {
    class ListEditorExRSS : ListEditor {
        ColumnHeader cdisable, cname, caddress, ccheckSchedule,
                     copenAction, coverrideSound, ccustomSound;

        public int SuggestedHeight
            => toolstrip.Height + 23 + list.Items.Count * (list.IntegralHeight + 4);

        public ListEditorExRSS() {
            InitializeMyComponent();
        }
        void InitializeMyComponent() {
            list.SuspendLayout();
            SuspendLayout();

            list.Columns.Clear();
            cvalue.Dispose();

            int pos = 0;
            cdisable        = AddColumn(pos++, "Disable", 50, HorizontalAlignment.Right);
            cname           = AddColumn(pos++, "Name", 120, HorizontalAlignment.Right);
            caddress        = AddColumn(pos++, "Address", 200);
            ccheckSchedule  = AddColumn(pos++, "Check schedule", 120);
            copenAction     = AddColumn(pos++, "Action", 260);
            coverrideSound  = AddColumn(pos++, "Override sound", 95);
            ccustomSound    = AddColumn(pos++, "Custom sound", 140, HorizontalAlignment.Right);
            
            list.AllowDrop = true;
            list.Items.RemovingItem += list_RemovingItem;
            list.DragEnter += list_DragEnter;

            //TODO: Implement importing/exporting feeds.
            EnableEdit = EnableMove =
            VisibleBackup = VisibleEdit = VisibleMove =
            AllowDuplicate = true;
            EnableBackup =
            EnableSettings = VisibleSettings = false;
            UpdateGUI();
            UpdateAutomatically = true;
            toolstrip.Items.Remove(btnEdit);

            ResumeLayout(false);
            PerformLayout();
        }
        
        void list_DragEnter(object sender, DragEventArgs e) {
            /*var formats = e.Data.GetFormats();
            if (formats.Contains(BOOKMARK)) {
                var mem = (MemoryStream)e.Data.GetData(BOOKMARK);
                StringBuilder sb = new StringBuilder();
                mem.PassBytes(b => sb.Append($"{b:x2}"));
                Clipboard.SetText(sb.ToString());
            }*/
        }
        void list_RemovingItem(object sender, ListViewItem item) {
            if (item is LVI_Ex x)
                Settings.feeds.Remove((Feed)x.Value);
        }

        internal void load() {
            list.Items.Clear();
            list.Items.AddRange(Settings.feeds.Select(f => new LVI_Ex(f)).ToArray());
        }
        internal void save() {
            Settings.feeds.Clear();
            Settings.feeds.AddRange(list.Items.OfType<LVI_Ex>().Select(e => (Feed)e.Value));
        }
        protected override IExposable newItemBondedValue(int index, IExposable copyFrom = null) {
            var sec = (Feed)copyFrom;
            var se = sec == null
                ? new Feed()
                : new Feed(sec);
            Settings.feeds.Insert(index, se);
            return se;
        }
    }
}
