using System.Windows.Forms;

namespace RoyalApps.Community.ExternalApps.WinForms.Demo;

partial class FormMain
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.MenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemExit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemApplication = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemEmbed = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemDetach = new System.Windows.Forms.ToolStripMenuItem();
            this.TabControlLeft = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.SplitContainerMain = new System.Windows.Forms.SplitContainer();
            this.SplitContainerTabs = new System.Windows.Forms.SplitContainer();
            this.TabControlRight = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.StripLabelQuickEmbed = new System.Windows.Forms.ToolStripLabel();
            this.StripTextBoxQuickEmbed = new System.Windows.Forms.ToolStripTextBox();
            this.StripDropDownButtonAdd = new System.Windows.Forms.ToolStripDropDownButton();
            this.StripButtonAddLeft = new System.Windows.Forms.ToolStripButton();
            this.StripButtonAddRight = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItemControl = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItemWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.TextBoxLog = new System.Windows.Forms.TextBox();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.MenuStrip.SuspendLayout();
            this.TabControlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainerMain)).BeginInit();
            this.SplitContainerMain.Panel1.SuspendLayout();
            this.SplitContainerMain.Panel2.SuspendLayout();
            this.SplitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainerTabs)).BeginInit();
            this.SplitContainerTabs.Panel1.SuspendLayout();
            this.SplitContainerTabs.Panel2.SuspendLayout();
            this.SplitContainerTabs.SuspendLayout();
            this.TabControlRight.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MenuStrip
            // 
            this.MenuStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemFile,
            this.MenuItemApplication});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Padding = new System.Windows.Forms.Padding(11, 5, 0, 5);
            this.MenuStrip.Size = new System.Drawing.Size(3185, 46);
            this.MenuStrip.TabIndex = 0;
            this.MenuStrip.Text = "menuStrip1";
            // 
            // MenuItemFile
            // 
            this.MenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemExit});
            this.MenuItemFile.Name = "MenuItemFile";
            this.MenuItemFile.Size = new System.Drawing.Size(71, 36);
            this.MenuItemFile.Text = "&File";
            // 
            // MenuItemExit
            // 
            this.MenuItemExit.Name = "MenuItemExit";
            this.MenuItemExit.Size = new System.Drawing.Size(184, 44);
            this.MenuItemExit.Text = "E&xit";
            this.MenuItemExit.Click += new System.EventHandler(this.MenuItemExit_Click);
            // 
            // MenuItemApplication
            // 
            this.MenuItemApplication.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItemEmbed,
            this.MenuItemDetach});
            this.MenuItemApplication.Name = "MenuItemApplication";
            this.MenuItemApplication.Size = new System.Drawing.Size(154, 36);
            this.MenuItemApplication.Text = "&Application";
            this.MenuItemApplication.DropDownOpening += new System.EventHandler(this.MenuItemApplication_DropDownOpening);
            // 
            // MenuItemEmbed
            // 
            this.MenuItemEmbed.Enabled = false;
            this.MenuItemEmbed.Name = "MenuItemEmbed";
            this.MenuItemEmbed.Size = new System.Drawing.Size(222, 44);
            this.MenuItemEmbed.Text = "&Embed";
            this.MenuItemEmbed.Click += new System.EventHandler(this.MenuItemEmbed_Click);
            // 
            // MenuItemDetach
            // 
            this.MenuItemDetach.Enabled = false;
            this.MenuItemDetach.Name = "MenuItemDetach";
            this.MenuItemDetach.Size = new System.Drawing.Size(222, 44);
            this.MenuItemDetach.Text = "&Detach";
            this.MenuItemDetach.Click += new System.EventHandler(this.MenuItemDetach_Click);
            // 
            // TabControlLeft
            // 
            this.TabControlLeft.Controls.Add(this.tabPage1);
            this.TabControlLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabControlLeft.Location = new System.Drawing.Point(0, 0);
            this.TabControlLeft.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.TabControlLeft.Name = "TabControlLeft";
            this.TabControlLeft.SelectedIndex = 0;
            this.TabControlLeft.Size = new System.Drawing.Size(1570, 1385);
            this.TabControlLeft.TabIndex = 1;
            this.TabControlLeft.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TabControl_MouseUp);
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(8, 46);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.tabPage1.Size = new System.Drawing.Size(1554, 1331);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // SplitContainerMain
            // 
            this.SplitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainerMain.Location = new System.Drawing.Point(0, 46);
            this.SplitContainerMain.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.SplitContainerMain.Name = "SplitContainerMain";
            this.SplitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // SplitContainerMain.Panel1
            // 
            this.SplitContainerMain.Panel1.Controls.Add(this.SplitContainerTabs);
            this.SplitContainerMain.Panel1.Controls.Add(this.toolStrip1);
            // 
            // SplitContainerMain.Panel2
            // 
            this.SplitContainerMain.Panel2.Controls.Add(this.TextBoxLog);
            this.SplitContainerMain.Size = new System.Drawing.Size(3185, 2049);
            this.SplitContainerMain.SplitterDistance = 1427;
            this.SplitContainerMain.SplitterWidth = 10;
            this.SplitContainerMain.TabIndex = 2;
            // 
            // SplitContainerTabs
            // 
            this.SplitContainerTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainerTabs.Location = new System.Drawing.Point(0, 42);
            this.SplitContainerTabs.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.SplitContainerTabs.Name = "SplitContainerTabs";
            // 
            // SplitContainerTabs.Panel1
            // 
            this.SplitContainerTabs.Panel1.Controls.Add(this.TabControlLeft);
            // 
            // SplitContainerTabs.Panel2
            // 
            this.SplitContainerTabs.Panel2.Controls.Add(this.TabControlRight);
            this.SplitContainerTabs.Size = new System.Drawing.Size(3185, 1385);
            this.SplitContainerTabs.SplitterDistance = 1570;
            this.SplitContainerTabs.SplitterWidth = 9;
            this.SplitContainerTabs.TabIndex = 3;
            // 
            // TabControlRight
            // 
            this.TabControlRight.Controls.Add(this.tabPage2);
            this.TabControlRight.Controls.Add(this.tabPage3);
            this.TabControlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabControlRight.Location = new System.Drawing.Point(0, 0);
            this.TabControlRight.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.TabControlRight.Name = "TabControlRight";
            this.TabControlRight.SelectedIndex = 0;
            this.TabControlRight.Size = new System.Drawing.Size(1606, 1385);
            this.TabControlRight.TabIndex = 0;
            this.TabControlRight.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TabControl_MouseUp);
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(8, 46);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.tabPage2.Size = new System.Drawing.Size(1590, 1331);
            this.tabPage2.TabIndex = 0;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(8, 46);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.tabPage3.Size = new System.Drawing.Size(1590, 1331);
            this.tabPage3.TabIndex = 1;
            this.tabPage3.Text = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StripLabelQuickEmbed,
            this.StripTextBoxQuickEmbed,
            this.StripDropDownButtonAdd});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.toolStrip1.Size = new System.Drawing.Size(3185, 42);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // StripLabelQuickEmbed
            // 
            this.StripLabelQuickEmbed.Name = "StripLabelQuickEmbed";
            this.StripLabelQuickEmbed.Size = new System.Drawing.Size(161, 36);
            this.StripLabelQuickEmbed.Text = "&Quick Embed:";
            // 
            // StripTextBoxQuickEmbed
            // 
            this.StripTextBoxQuickEmbed.Name = "StripTextBoxQuickEmbed";
            this.StripTextBoxQuickEmbed.Size = new System.Drawing.Size(429, 42);
            this.StripTextBoxQuickEmbed.Text = "c:\\windows\\System32\\cmd.exe";
            // 
            // StripDropDownButtonAdd
            // 
            this.StripDropDownButtonAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.StripDropDownButtonAdd.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StripButtonAddLeft,
            this.StripButtonAddRight,
            this.toolStripSeparator1,
            this.MenuItemControl,
            this.MenuItemWindow});
            this.StripDropDownButtonAdd.Image = ((System.Drawing.Image)(resources.GetObject("StripDropDownButtonAdd.Image")));
            this.StripDropDownButtonAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.StripDropDownButtonAdd.Name = "StripDropDownButtonAdd";
            this.StripDropDownButtonAdd.Size = new System.Drawing.Size(79, 36);
            this.StripDropDownButtonAdd.Text = "&Add";
            // 
            // StripButtonAddLeft
            // 
            this.StripButtonAddLeft.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.StripButtonAddLeft.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.StripButtonAddLeft.Name = "StripButtonAddLeft";
            this.StripButtonAddLeft.Size = new System.Drawing.Size(108, 36);
            this.StripButtonAddLeft.Text = "Add &Left";
            this.StripButtonAddLeft.Click += new System.EventHandler(this.StripButtonAddLeft_Click);
            // 
            // StripButtonAddRight
            // 
            this.StripButtonAddRight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.StripButtonAddRight.Image = ((System.Drawing.Image)(resources.GetObject("StripButtonAddRight.Image")));
            this.StripButtonAddRight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.StripButtonAddRight.Name = "StripButtonAddRight";
            this.StripButtonAddRight.Size = new System.Drawing.Size(124, 36);
            this.StripButtonAddRight.Text = "Add &Right";
            this.StripButtonAddRight.Click += new System.EventHandler(this.StripButtonAddRight_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(409, 6);
            // 
            // MenuItemControl
            // 
            this.MenuItemControl.Name = "MenuItemControl";
            this.MenuItemControl.Size = new System.Drawing.Size(412, 44);
            this.MenuItemControl.Text = "Embed Method: &Control";
            this.MenuItemControl.Click += new System.EventHandler(this.MenuItemControl_Click);
            // 
            // MenuItemWindow
            // 
            this.MenuItemWindow.Checked = true;
            this.MenuItemWindow.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MenuItemWindow.Name = "MenuItemWindow";
            this.MenuItemWindow.Size = new System.Drawing.Size(412, 44);
            this.MenuItemWindow.Text = "Embed Method: &Window";
            this.MenuItemWindow.Click += new System.EventHandler(this.MenuItemWindow_Click);
            // 
            // TextBoxLog
            // 
            this.TextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextBoxLog.Location = new System.Drawing.Point(0, 0);
            this.TextBoxLog.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.TextBoxLog.Multiline = true;
            this.TextBoxLog.Name = "TextBoxLog";
            this.TextBoxLog.ReadOnly = true;
            this.TextBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TextBoxLog.Size = new System.Drawing.Size(3185, 612);
            this.TextBoxLog.TabIndex = 0;
            // 
            // StatusStrip
            // 
            this.StatusStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 2095);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Padding = new System.Windows.Forms.Padding(2, 0, 30, 0);
            this.StatusStrip.Size = new System.Drawing.Size(3185, 42);
            this.StatusStrip.TabIndex = 3;
            this.StatusStrip.Text = "statusStrip1";
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(93, 32);
            this.StatusLabel.Text = "Status...";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(3185, 2137);
            this.Controls.Add(this.SplitContainerMain);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.MenuStrip);
            this.MainMenuStrip = this.MenuStrip;
            this.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.Name = "FormMain";
            this.Text = "External Apps Demo";
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.TabControlLeft.ResumeLayout(false);
            this.SplitContainerMain.Panel1.ResumeLayout(false);
            this.SplitContainerMain.Panel1.PerformLayout();
            this.SplitContainerMain.Panel2.ResumeLayout(false);
            this.SplitContainerMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainerMain)).EndInit();
            this.SplitContainerMain.ResumeLayout(false);
            this.SplitContainerTabs.Panel1.ResumeLayout(false);
            this.SplitContainerTabs.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainerTabs)).EndInit();
            this.SplitContainerTabs.ResumeLayout(false);
            this.TabControlRight.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    private System.Windows.Forms.ToolStripMenuItem MenuItemControl;

    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;

    private System.Windows.Forms.ToolStripDropDownButton StripDropDownButtonAdd;

    private System.Windows.Forms.ToolStripButton StripButtonAddRight;

    private System.Windows.Forms.SplitContainer SplitContainerTabs;
    private System.Windows.Forms.TabControl TabControlRight;
    private System.Windows.Forms.TabPage tabPage2;
    private System.Windows.Forms.TabPage tabPage3;

    private System.Windows.Forms.ToolStripButton StripButtonAddLeft;

    private System.Windows.Forms.ToolStripTextBox StripTextBoxQuickEmbed;

    private System.Windows.Forms.ToolStripLabel StripLabelQuickEmbed;

    private System.Windows.Forms.ToolStrip toolStrip1;

    private System.Windows.Forms.TextBox TextBoxLog;

    private System.Windows.Forms.ToolStripStatusLabel StatusLabel;

    private System.Windows.Forms.SplitContainer SplitContainerMain;
    private System.Windows.Forms.StatusStrip StatusStrip;

    #endregion

    private System.Windows.Forms.MenuStrip MenuStrip;
    private System.Windows.Forms.ToolStripMenuItem MenuItemFile;
    private System.Windows.Forms.ToolStripMenuItem MenuItemExit;
    private System.Windows.Forms.ToolStripMenuItem MenuItemApplication;
    private System.Windows.Forms.ToolStripMenuItem MenuItemEmbed;
    private System.Windows.Forms.TabControl TabControlLeft;
    private System.Windows.Forms.TabPage tabPage1;
    private ToolStripMenuItem MenuItemDetach;
    private ToolStripMenuItem MenuItemWindow;
}