﻿namespace HZDUtility
{
    partial class Main
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnUpdateDefaultMaps = new System.Windows.Forms.Button();
            this.lbOutfits = new HZDUtility.ListBoxNF();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.clbModels = new System.Windows.Forms.CheckedListBox();
            this.ssMain = new System.Windows.Forms.StatusStrip();
            this.tssStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnPatch = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnUpdateDefaultMaps
            // 
            this.btnUpdateDefaultMaps.Location = new System.Drawing.Point(12, 12);
            this.btnUpdateDefaultMaps.Name = "btnUpdateDefaultMaps";
            this.btnUpdateDefaultMaps.Size = new System.Drawing.Size(172, 28);
            this.btnUpdateDefaultMaps.TabIndex = 0;
            this.btnUpdateDefaultMaps.Text = "Update Default Maps";
            this.btnUpdateDefaultMaps.UseVisualStyleBackColor = true;
            this.btnUpdateDefaultMaps.Click += new System.EventHandler(this.btnUpdateDefaultMaps_Click);
            // 
            // lbOutfits
            // 
            this.lbOutfits.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbOutfits.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbOutfits.FormattingEnabled = true;
            this.lbOutfits.IntegralHeight = false;
            this.lbOutfits.ItemHeight = 15;
            this.lbOutfits.Location = new System.Drawing.Point(0, 18);
            this.lbOutfits.Name = "lbOutfits";
            this.lbOutfits.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbOutfits.Size = new System.Drawing.Size(300, 511);
            this.lbOutfits.TabIndex = 1;
            this.lbOutfits.SelectedValueChanged += new System.EventHandler(this.lbOutfits_SelectedValueChanged);
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.Location = new System.Drawing.Point(12, 46);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.label1);
            this.splitContainer.Panel1.Controls.Add(this.lbOutfits);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.label2);
            this.splitContainer.Panel2.Controls.Add(this.clbModels);
            this.splitContainer.Size = new System.Drawing.Size(725, 529);
            this.splitContainer.SplitterDistance = 300;
            this.splitContainer.TabIndex = 2;
            this.splitContainer.Text = "splitContainer1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Outfits";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(92, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "Model Mapping";
            // 
            // clbModels
            // 
            this.clbModels.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clbModels.CheckOnClick = true;
            this.clbModels.FormattingEnabled = true;
            this.clbModels.IntegralHeight = false;
            this.clbModels.Location = new System.Drawing.Point(0, 18);
            this.clbModels.Name = "clbModels";
            this.clbModels.Size = new System.Drawing.Size(421, 511);
            this.clbModels.TabIndex = 0;
            this.clbModels.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbModels_ItemCheck);
            // 
            // ssMain
            // 
            this.ssMain.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.ssMain.Location = new System.Drawing.Point(0, 578);
            this.ssMain.Name = "ssMain";
            this.ssMain.Size = new System.Drawing.Size(749, 22);
            this.ssMain.TabIndex = 3;
            // 
            // tssStatus
            // 
            this.tssStatus.Name = "tssStatus";
            this.tssStatus.Size = new System.Drawing.Size(53, 17);
            this.tssStatus.Text = "tssStatus";
            // 
            // btnPatch
            // 
            this.btnPatch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPatch.Location = new System.Drawing.Point(565, 12);
            this.btnPatch.Name = "btnPatch";
            this.btnPatch.Size = new System.Drawing.Size(172, 28);
            this.btnPatch.TabIndex = 4;
            this.btnPatch.Text = "Create Patch";
            this.btnPatch.UseVisualStyleBackColor = true;
            this.btnPatch.Click += new System.EventHandler(this.btnPatch_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(749, 600);
            this.Controls.Add(this.btnPatch);
            this.Controls.Add(this.ssMain);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.btnUpdateDefaultMaps);
            this.DoubleBuffered = true;
            this.Name = "Main";
            this.Text = "Outfit Changer";
            this.Load += new System.EventHandler(this.Main_Load);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUpdateDefaultMaps;
        private ListBoxNF lbOutfits;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.CheckedListBox clbModels;
        private System.Windows.Forms.StatusStrip ssMain;
        private System.Windows.Forms.ToolStripStatusLabel tssStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnPatch;
    }
}

