namespace Dynammo.Simulator
{
    partial class SimulatorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimulatorForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.networkStateTreeView = new System.Windows.Forms.TreeView();
            this.mainImageList = new System.Windows.Forms.ImageList(this.components);
            this.panel2 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.networkNodeInfoPanel = new System.Windows.Forms.Panel();
            this.nodeUsernameLabel = new System.Windows.Forms.Label();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.nodeIPAddressLabel = new System.Windows.Forms.Label();
            this.nodeNameAddressLabel = new System.Windows.Forms.Label();
            this.panel66 = new System.Windows.Forms.Panel();
            this.logWindowTabber = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.logVerbosityComboBox = new System.Windows.Forms.ComboBox();
            this.logTreeView = new System.Windows.Forms.TreeView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.startSimulationToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.pauseSimulationToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.stopSimulationToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.treeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.takeControlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.worldPanel = new Dynammo.Common.DoubleBufferedPanel();
            this.clientBandwidthPanel = new Dynammo.Common.GraphPanel();
            this.arbitratorBandwidthPanel = new Dynammo.Common.GraphPanel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.networkNodeInfoPanel.SuspendLayout();
            this.panel66.SuspendLayout();
            this.logWindowTabber.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.treeContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.logWindowTabber);
            this.splitContainer1.Size = new System.Drawing.Size(1046, 609);
            this.splitContainer1.SplitterDistance = 456;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.networkStateTreeView);
            this.splitContainer2.Panel1.Controls.Add(this.panel2);
            this.splitContainer2.Panel1.Controls.Add(this.networkNodeInfoPanel);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.panel66);
            this.splitContainer2.Size = new System.Drawing.Size(1046, 456);
            this.splitContainer2.SplitterDistance = 176;
            this.splitContainer2.TabIndex = 0;
            // 
            // networkStateTreeView
            // 
            this.networkStateTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.networkStateTreeView.FullRowSelect = true;
            this.networkStateTreeView.ImageIndex = 0;
            this.networkStateTreeView.ImageList = this.mainImageList;
            this.networkStateTreeView.Location = new System.Drawing.Point(0, 19);
            this.networkStateTreeView.Name = "networkStateTreeView";
            this.networkStateTreeView.SelectedImageIndex = 0;
            this.networkStateTreeView.Size = new System.Drawing.Size(176, 372);
            this.networkStateTreeView.TabIndex = 2;
            this.networkStateTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.networkStateTreeView_KeyDown);
            this.networkStateTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.networkStateTreeView_MouseDown);
            // 
            // mainImageList
            // 
            this.mainImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("mainImageList.ImageStream")));
            this.mainImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.mainImageList.Images.SetKeyName(0, "comment.png");
            this.mainImageList.Images.SetKeyName(1, "information.png");
            this.mainImageList.Images.SetKeyName(2, "exclamation.png");
            this.mainImageList.Images.SetKeyName(3, "stop.png");
            this.mainImageList.Images.SetKeyName(4, "database.png");
            this.mainImageList.Images.SetKeyName(5, "computer.png");
            this.mainImageList.Images.SetKeyName(6, "server.png");
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(176, 19);
            this.panel2.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label4.Location = new System.Drawing.Point(147, 2);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(25, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "::::::";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label2.Location = new System.Drawing.Point(2, 2);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Network State";
            // 
            // networkNodeInfoPanel
            // 
            this.networkNodeInfoPanel.Controls.Add(this.nodeUsernameLabel);
            this.networkNodeInfoPanel.Controls.Add(this.panel6);
            this.networkNodeInfoPanel.Controls.Add(this.panel5);
            this.networkNodeInfoPanel.Controls.Add(this.panel4);
            this.networkNodeInfoPanel.Controls.Add(this.nodeIPAddressLabel);
            this.networkNodeInfoPanel.Controls.Add(this.nodeNameAddressLabel);
            this.networkNodeInfoPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.networkNodeInfoPanel.Location = new System.Drawing.Point(0, 391);
            this.networkNodeInfoPanel.Name = "networkNodeInfoPanel";
            this.networkNodeInfoPanel.Size = new System.Drawing.Size(176, 65);
            this.networkNodeInfoPanel.TabIndex = 4;
            this.networkNodeInfoPanel.Visible = false;
            // 
            // nodeUsernameLabel
            // 
            this.nodeUsernameLabel.AutoSize = true;
            this.nodeUsernameLabel.Location = new System.Drawing.Point(8, 41);
            this.nodeUsernameLabel.Name = "nodeUsernameLabel";
            this.nodeUsernameLabel.Size = new System.Drawing.Size(0, 13);
            this.nodeUsernameLabel.TabIndex = 5;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panel6.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel6.Location = new System.Drawing.Point(1, 64);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(174, 1);
            this.panel6.TabIndex = 4;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panel5.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel5.Location = new System.Drawing.Point(0, 0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1, 65);
            this.panel5.TabIndex = 3;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(175, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1, 65);
            this.panel4.TabIndex = 2;
            // 
            // nodeIPAddressLabel
            // 
            this.nodeIPAddressLabel.AutoSize = true;
            this.nodeIPAddressLabel.Location = new System.Drawing.Point(8, 26);
            this.nodeIPAddressLabel.Name = "nodeIPAddressLabel";
            this.nodeIPAddressLabel.Size = new System.Drawing.Size(0, 13);
            this.nodeIPAddressLabel.TabIndex = 1;
            // 
            // nodeNameAddressLabel
            // 
            this.nodeNameAddressLabel.AutoSize = true;
            this.nodeNameAddressLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodeNameAddressLabel.Location = new System.Drawing.Point(8, 9);
            this.nodeNameAddressLabel.Name = "nodeNameAddressLabel";
            this.nodeNameAddressLabel.Size = new System.Drawing.Size(58, 13);
            this.nodeNameAddressLabel.TabIndex = 0;
            this.nodeNameAddressLabel.Text = "Client #1";
            // 
            // panel66
            // 
            this.panel66.AutoScroll = true;
            this.panel66.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel66.Controls.Add(this.worldPanel);
            this.panel66.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel66.Location = new System.Drawing.Point(0, 0);
            this.panel66.Name = "panel66";
            this.panel66.Size = new System.Drawing.Size(866, 456);
            this.panel66.TabIndex = 0;
            // 
            // logWindowTabber
            // 
            this.logWindowTabber.Controls.Add(this.tabPage1);
            this.logWindowTabber.Controls.Add(this.tabPage2);
            this.logWindowTabber.Controls.Add(this.tabPage3);
            this.logWindowTabber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logWindowTabber.Location = new System.Drawing.Point(0, 0);
            this.logWindowTabber.Name = "logWindowTabber";
            this.logWindowTabber.SelectedIndex = 0;
            this.logWindowTabber.Size = new System.Drawing.Size(1046, 149);
            this.logWindowTabber.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.logVerbosityComboBox);
            this.tabPage1.Controls.Add(this.logTreeView);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(1038, 123);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Simulator Log";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // logVerbosityComboBox
            // 
            this.logVerbosityComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.logVerbosityComboBox.FormattingEnabled = true;
            this.logVerbosityComboBox.Items.AddRange(new object[] {
            "Highest Verbosity",
            "High Verbosity",
            "Normal Verbosity",
            "Low Verbosity",
            "Lowest Verbosity"});
            this.logVerbosityComboBox.Location = new System.Drawing.Point(894, 6);
            this.logVerbosityComboBox.Name = "logVerbosityComboBox";
            this.logVerbosityComboBox.Size = new System.Drawing.Size(121, 21);
            this.logVerbosityComboBox.TabIndex = 1;
            this.logVerbosityComboBox.Text = "Normal Verbosity";
            this.logVerbosityComboBox.SelectedIndexChanged += new System.EventHandler(this.logVerbosityComboBox_SelectedIndexChanged);
            // 
            // logTreeView
            // 
            this.logTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logTreeView.Font = new System.Drawing.Font("Lucida Console", 7.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logTreeView.ImageIndex = 0;
            this.logTreeView.ImageList = this.mainImageList;
            this.logTreeView.Location = new System.Drawing.Point(0, 0);
            this.logTreeView.Name = "logTreeView";
            this.logTreeView.SelectedImageIndex = 0;
            this.logTreeView.Size = new System.Drawing.Size(1038, 123);
            this.logTreeView.TabIndex = 2;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.clientBandwidthPanel);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(1038, 123);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Client Statistics";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.arbitratorBandwidthPanel);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(1038, 123);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Arbitrator Statistics";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startSimulationToolStripButton,
            this.pauseSimulationToolStripButton,
            this.stopSimulationToolStripButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1046, 25);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip1";
            // 
            // startSimulationToolStripButton
            // 
            this.startSimulationToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.startSimulationToolStripButton.Image = global::Dynammo.Simulator.Properties.Resources.control_play;
            this.startSimulationToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.startSimulationToolStripButton.Name = "startSimulationToolStripButton";
            this.startSimulationToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.startSimulationToolStripButton.Text = "Start Simulation";
            this.startSimulationToolStripButton.Click += new System.EventHandler(this.startSimulationToolStripButton_Click);
            // 
            // pauseSimulationToolStripButton
            // 
            this.pauseSimulationToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.pauseSimulationToolStripButton.Enabled = false;
            this.pauseSimulationToolStripButton.Image = global::Dynammo.Simulator.Properties.Resources.control_pause;
            this.pauseSimulationToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.pauseSimulationToolStripButton.Name = "pauseSimulationToolStripButton";
            this.pauseSimulationToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.pauseSimulationToolStripButton.Text = "Pause Simulation";
            this.pauseSimulationToolStripButton.Click += new System.EventHandler(this.pauseSimulationToolStripButton_Click);
            // 
            // stopSimulationToolStripButton
            // 
            this.stopSimulationToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.stopSimulationToolStripButton.Enabled = false;
            this.stopSimulationToolStripButton.Image = global::Dynammo.Simulator.Properties.Resources.control_stop;
            this.stopSimulationToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.stopSimulationToolStripButton.Name = "stopSimulationToolStripButton";
            this.stopSimulationToolStripButton.Size = new System.Drawing.Size(23, 22);
            this.stopSimulationToolStripButton.Text = "Stop Simulation";
            this.stopSimulationToolStripButton.Click += new System.EventHandler(this.stopSimulationToolStripButton_Click);
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 150;
            this.updateTimer.Tick += new System.EventHandler(this.updateTimer_Tick);
            // 
            // treeContextMenuStrip
            // 
            this.treeContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.takeControlToolStripMenuItem,
            this.disconnectToolStripMenuItem});
            this.treeContextMenuStrip.Name = "treeContextMenuStrip";
            this.treeContextMenuStrip.Size = new System.Drawing.Size(143, 48);
            // 
            // takeControlToolStripMenuItem
            // 
            this.takeControlToolStripMenuItem.Name = "takeControlToolStripMenuItem";
            this.takeControlToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.takeControlToolStripMenuItem.Text = "Take Control";
            this.takeControlToolStripMenuItem.Click += new System.EventHandler(this.takeControlToolStripMenuItem_Click);
            // 
            // disconnectToolStripMenuItem
            // 
            this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.disconnectToolStripMenuItem.Text = "Disconnect";
            this.disconnectToolStripMenuItem.Click += new System.EventHandler(this.disconnectToolStripMenuItem_Click);
            // 
            // worldPanel
            // 
            this.worldPanel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.worldPanel.Location = new System.Drawing.Point(0, 0);
            this.worldPanel.Name = "worldPanel";
            this.worldPanel.Size = new System.Drawing.Size(10, 10);
            this.worldPanel.TabIndex = 0;
            this.worldPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.worldPanel_Paint);
            // 
            // clientBandwidthPanel
            // 
            this.clientBandwidthPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clientBandwidthPanel.Location = new System.Drawing.Point(0, 0);
            this.clientBandwidthPanel.Name = "clientBandwidthPanel";
            this.clientBandwidthPanel.Size = new System.Drawing.Size(1038, 123);
            this.clientBandwidthPanel.TabIndex = 1;
            // 
            // arbitratorBandwidthPanel
            // 
            this.arbitratorBandwidthPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.arbitratorBandwidthPanel.Location = new System.Drawing.Point(0, 0);
            this.arbitratorBandwidthPanel.Name = "arbitratorBandwidthPanel";
            this.arbitratorBandwidthPanel.Size = new System.Drawing.Size(1038, 123);
            this.arbitratorBandwidthPanel.TabIndex = 1;
            // 
            // SimulatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1046, 634);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip);
            this.Name = "SimulatorForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Dynammo Simulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SimulatorForm_FormClosing);
            this.Load += new System.EventHandler(this.SimulatorForm_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.networkNodeInfoPanel.ResumeLayout(false);
            this.networkNodeInfoPanel.PerformLayout();
            this.panel66.ResumeLayout(false);
            this.logWindowTabber.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.treeContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView networkStateTreeView;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel66;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton startSimulationToolStripButton;
        private System.Windows.Forms.ToolStripButton pauseSimulationToolStripButton;
        private System.Windows.Forms.ToolStripButton stopSimulationToolStripButton;
        private System.Windows.Forms.ImageList mainImageList;
        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel networkNodeInfoPanel;
        private System.Windows.Forms.Label nodeIPAddressLabel;
        private System.Windows.Forms.Label nodeNameAddressLabel;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel4;
        private Dynammo.Common.DoubleBufferedPanel worldPanel;
        private System.Windows.Forms.Label nodeUsernameLabel;
        private System.Windows.Forms.TabControl logWindowTabber;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TreeView logTreeView;
        private Common.GraphPanel clientBandwidthPanel;
        private Common.GraphPanel arbitratorBandwidthPanel;
        private System.Windows.Forms.ComboBox logVerbosityComboBox;
        private System.Windows.Forms.ContextMenuStrip treeContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem takeControlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
    }
}

