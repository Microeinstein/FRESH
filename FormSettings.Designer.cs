namespace FRESH {
    partial class FormSettings {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabFeeds = new System.Windows.Forms.TabPage();
            this.editor = new FRESH.ListEditorExRSS();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.tblSettings = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSound = new System.Windows.Forms.TextBox();
            this.numMax = new System.Windows.Forms.NumericUpDown();
            this.chkDisableNtf = new System.Windows.Forms.CheckBox();
            this.chkDisableSnd = new System.Windows.Forms.CheckBox();
            this.tblMain = new System.Windows.Forms.TableLayoutPanel();
            this.flwActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.tabs.SuspendLayout();
            this.tabFeeds.SuspendLayout();
            this.tabSettings.SuspendLayout();
            this.tblSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMax)).BeginInit();
            this.tblMain.SuspendLayout();
            this.flwActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabs
            // 
            this.tabs.Controls.Add(this.tabFeeds);
            this.tabs.Controls.Add(this.tabSettings);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(3, 3);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1002, 227);
            this.tabs.TabIndex = 0;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
            // 
            // tabFeeds
            // 
            this.tabFeeds.Controls.Add(this.editor);
            this.tabFeeds.Location = new System.Drawing.Point(4, 24);
            this.tabFeeds.Name = "tabFeeds";
            this.tabFeeds.Size = new System.Drawing.Size(994, 199);
            this.tabFeeds.TabIndex = 0;
            this.tabFeeds.Text = "Feeds";
            this.tabFeeds.UseVisualStyleBackColor = true;
            // 
            // editor
            // 
            this.editor.AllowDuplicate = true;
            this.editor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editor.EditAfterDuplicate = false;
            this.editor.EnableBackup = true;
            this.editor.EnableEdit = true;
            this.editor.EnableLoad = false;
            this.editor.EnableMove = true;
            this.editor.EnableSettings = false;
            this.editor.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.editor.Location = new System.Drawing.Point(0, 0);
            this.editor.Margin = new System.Windows.Forms.Padding(0);
            this.editor.Name = "editor";
            this.editor.Padding = new System.Windows.Forms.Padding(0, 2, 2, 2);
            this.editor.Size = new System.Drawing.Size(994, 199);
            this.editor.TabIndex = 0;
            this.editor.VisibleBackup = false;
            this.editor.VisibleEdit = true;
            this.editor.VisibleLoad = false;
            this.editor.VisibleMove = true;
            this.editor.VisibleSettings = false;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.tblSettings);
            this.tabSettings.Location = new System.Drawing.Point(4, 24);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabSettings.Size = new System.Drawing.Size(994, 199);
            this.tabSettings.TabIndex = 1;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // tblSettings
            // 
            this.tblSettings.ColumnCount = 2;
            this.tblSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tblSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblSettings.Controls.Add(this.label1, 0, 0);
            this.tblSettings.Controls.Add(this.label3, 0, 1);
            this.tblSettings.Controls.Add(this.txtSound, 1, 0);
            this.tblSettings.Controls.Add(this.numMax, 1, 1);
            this.tblSettings.Controls.Add(this.chkDisableNtf, 0, 2);
            this.tblSettings.Controls.Add(this.chkDisableSnd, 0, 3);
            this.tblSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblSettings.Location = new System.Drawing.Point(3, 3);
            this.tblSettings.Name = "tblSettings";
            this.tblSettings.RowCount = 5;
            this.tblSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblSettings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblSettings.Size = new System.Drawing.Size(988, 193);
            this.tblSettings.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(47, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Default sound effect:";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 32);
            this.label3.Margin = new System.Windows.Forms.Padding(3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(161, 15);
            this.label3.TabIndex = 1;
            this.label3.Text = "Maximum elements per feed:";
            // 
            // txtSound
            // 
            this.txtSound.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtSound.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.txtSound.Location = new System.Drawing.Point(170, 2);
            this.txtSound.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtSound.Name = "txtSound";
            this.txtSound.ReadOnly = true;
            this.txtSound.Size = new System.Drawing.Size(326, 23);
            this.txtSound.TabIndex = 2;
            // 
            // numMax
            // 
            this.numMax.Font = new System.Drawing.Font("Consolas", 9F);
            this.numMax.Location = new System.Drawing.Point(170, 29);
            this.numMax.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numMax.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.numMax.Name = "numMax";
            this.numMax.Size = new System.Drawing.Size(98, 22);
            this.numMax.TabIndex = 4;
            this.numMax.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // chkDisableNtf
            // 
            this.chkDisableNtf.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkDisableNtf.AutoSize = true;
            this.chkDisableNtf.Location = new System.Drawing.Point(10, 55);
            this.chkDisableNtf.Margin = new System.Windows.Forms.Padding(10, 2, 2, 2);
            this.chkDisableNtf.Name = "chkDisableNtf";
            this.chkDisableNtf.Size = new System.Drawing.Size(128, 19);
            this.chkDisableNtf.TabIndex = 3;
            this.chkDisableNtf.Text = "Disable notification";
            this.chkDisableNtf.UseVisualStyleBackColor = true;
            // 
            // chkDisableSnd
            // 
            this.chkDisableSnd.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkDisableSnd.AutoSize = true;
            this.chkDisableSnd.Location = new System.Drawing.Point(10, 78);
            this.chkDisableSnd.Margin = new System.Windows.Forms.Padding(10, 2, 2, 2);
            this.chkDisableSnd.Name = "chkDisableSnd";
            this.chkDisableSnd.Size = new System.Drawing.Size(100, 19);
            this.chkDisableSnd.TabIndex = 3;
            this.chkDisableSnd.Text = "Disable sound";
            this.chkDisableSnd.UseVisualStyleBackColor = true;
            // 
            // tblMain
            // 
            this.tblMain.ColumnCount = 1;
            this.tblMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMain.Controls.Add(this.tabs, 0, 0);
            this.tblMain.Controls.Add(this.flwActions, 0, 1);
            this.tblMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMain.Location = new System.Drawing.Point(0, 0);
            this.tblMain.Name = "tblMain";
            this.tblMain.RowCount = 2;
            this.tblMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblMain.Size = new System.Drawing.Size(1008, 262);
            this.tblMain.TabIndex = 1;
            // 
            // flwActions
            // 
            this.flwActions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.flwActions.AutoSize = true;
            this.flwActions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flwActions.Controls.Add(this.btnOK);
            this.flwActions.Controls.Add(this.btnCancel);
            this.flwActions.Controls.Add(this.btnReload);
            this.flwActions.Controls.Add(this.btnApply);
            this.flwActions.Location = new System.Drawing.Point(684, 233);
            this.flwActions.Margin = new System.Windows.Forms.Padding(0);
            this.flwActions.Name = "flwActions";
            this.flwActions.Size = new System.Drawing.Size(324, 29);
            this.flwActions.TabIndex = 5;
            this.flwActions.WrapContents = false;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(3, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(84, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(165, 3);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(75, 23);
            this.btnReload.TabIndex = 2;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(246, 3);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 3;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // FormSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 262);
            this.Controls.Add(this.tblMain);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(580, 300);
            this.Name = "FormSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FRESH";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.form_formClosing);
            this.tabs.ResumeLayout(false);
            this.tabFeeds.ResumeLayout(false);
            this.tabSettings.ResumeLayout(false);
            this.tblSettings.ResumeLayout(false);
            this.tblSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMax)).EndInit();
            this.tblMain.ResumeLayout(false);
            this.tblMain.PerformLayout();
            this.flwActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabFeeds;
        private System.Windows.Forms.TabPage tabSettings;
        private ListEditorExRSS editor;
        private System.Windows.Forms.CheckBox chkDisableNtf;
        private System.Windows.Forms.TextBox txtSound;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tblSettings;
        private System.Windows.Forms.TableLayoutPanel tblMain;
        private System.Windows.Forms.FlowLayoutPanel flwActions;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.NumericUpDown numMax;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkDisableSnd;
    }
}

