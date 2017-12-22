namespace image_segm
{
    partial class Form1
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
            this.srcPicBox = new System.Windows.Forms.PictureBox();
            this.resPicBox = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filteringToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.processToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.processSimpleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.processMediumToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.processManiacToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.srcPicBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.resPicBox)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // srcPicBox
            // 
            this.srcPicBox.Location = new System.Drawing.Point(12, 24);
            this.srcPicBox.Name = "srcPicBox";
            this.srcPicBox.Size = new System.Drawing.Size(590, 613);
            this.srcPicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.srcPicBox.TabIndex = 0;
            this.srcPicBox.TabStop = false;
            // 
            // resPicBox
            // 
            this.resPicBox.Location = new System.Drawing.Point(608, 24);
            this.resPicBox.Name = "resPicBox";
            this.resPicBox.Size = new System.Drawing.Size(590, 613);
            this.resPicBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.resPicBox.TabIndex = 1;
            this.resPicBox.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.filteringToolStripMenuItem,
            this.processToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1210, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.openToolStripMenuItem.Text = "Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.saveToolStripMenuItem.Text = "Save...";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // filteringToolStripMenuItem
            // 
            this.filteringToolStripMenuItem.Name = "filteringToolStripMenuItem";
            this.filteringToolStripMenuItem.Size = new System.Drawing.Size(62, 20);
            this.filteringToolStripMenuItem.Text = "Filtering";
            // 
            // processToolStripMenuItem
            // 
            this.processToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.processSimpleToolStripMenuItem,
            this.processMediumToolStripMenuItem,
            this.processManiacToolStripMenuItem});
            this.processToolStripMenuItem.Name = "processToolStripMenuItem";
            this.processToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.processToolStripMenuItem.Text = "Process";
            // 
            // processSimpleToolStripMenuItem
            // 
            this.processSimpleToolStripMenuItem.Name = "processSimpleToolStripMenuItem";
            this.processSimpleToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.processSimpleToolStripMenuItem.Text = "Process simple";
            this.processSimpleToolStripMenuItem.Click += new System.EventHandler(this.processSimpleToolStripMenuItem_Click);
            // 
            // processMediumToolStripMenuItem
            // 
            this.processMediumToolStripMenuItem.Name = "processMediumToolStripMenuItem";
            this.processMediumToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.processMediumToolStripMenuItem.Text = "Process medium";
            this.processMediumToolStripMenuItem.Click += new System.EventHandler(this.processMediumToolStripMenuItem_Click);
            // 
            // processManiacToolStripMenuItem
            // 
            this.processManiacToolStripMenuItem.Name = "processManiacToolStripMenuItem";
            this.processManiacToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.processManiacToolStripMenuItem.Text = "Process maniac";
            this.processManiacToolStripMenuItem.Click += new System.EventHandler(this.processManiacToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1210, 660);
            this.Controls.Add(this.resPicBox);
            this.Controls.Add(this.srcPicBox);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.srcPicBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.resPicBox)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox srcPicBox;
        private System.Windows.Forms.PictureBox resPicBox;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filteringToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processSimpleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processMediumToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processManiacToolStripMenuItem;
    }
}

