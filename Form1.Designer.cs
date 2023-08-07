namespace TestProcese
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
            this.btnStartSnapshot = new System.Windows.Forms.Button();
            this.btnStopSnapshot = new System.Windows.Forms.Button();
            this.tbMes = new System.Windows.Forms.TextBox();
            this.btnLookForRansomware = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnStartSnapshot
            // 
            this.btnStartSnapshot.Location = new System.Drawing.Point(48, 46);
            this.btnStartSnapshot.Name = "btnStartSnapshot";
            this.btnStartSnapshot.Size = new System.Drawing.Size(141, 53);
            this.btnStartSnapshot.TabIndex = 0;
            this.btnStartSnapshot.Text = "Start Snapshot";
            this.btnStartSnapshot.UseVisualStyleBackColor = true;
            this.btnStartSnapshot.Click += new System.EventHandler(this.btnStartSnapshot_Click);
            // 
            // btnStopSnapshot
            // 
            this.btnStopSnapshot.Location = new System.Drawing.Point(195, 46);
            this.btnStopSnapshot.Name = "btnStopSnapshot";
            this.btnStopSnapshot.Size = new System.Drawing.Size(141, 53);
            this.btnStopSnapshot.TabIndex = 2;
            this.btnStopSnapshot.Text = "Stop Snapshot";
            this.btnStopSnapshot.UseVisualStyleBackColor = true;
            this.btnStopSnapshot.Click += new System.EventHandler(this.btnStopSnapshot_Click);
            // 
            // tbMes
            // 
            this.tbMes.Dock = System.Windows.Forms.DockStyle.Right;
            this.tbMes.Location = new System.Drawing.Point(398, 0);
            this.tbMes.Multiline = true;
            this.tbMes.Name = "tbMes";
            this.tbMes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbMes.Size = new System.Drawing.Size(402, 738);
            this.tbMes.TabIndex = 3;
            // 
            // btnLookForRansomware
            // 
            this.btnLookForRansomware.Location = new System.Drawing.Point(48, 106);
            this.btnLookForRansomware.Name = "btnLookForRansomware";
            this.btnLookForRansomware.Size = new System.Drawing.Size(288, 53);
            this.btnLookForRansomware.TabIndex = 4;
            this.btnLookForRansomware.Text = "Look for Ransomware";
            this.btnLookForRansomware.UseVisualStyleBackColor = true;
            this.btnLookForRansomware.Click += new System.EventHandler(this.btnLookForRansomware_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 738);
            this.Controls.Add(this.btnLookForRansomware);
            this.Controls.Add(this.tbMes);
            this.Controls.Add(this.btnStopSnapshot);
            this.Controls.Add(this.btnStartSnapshot);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStartSnapshot;
        private System.Windows.Forms.Button btnStopSnapshot;
        private System.Windows.Forms.TextBox tbMes;
        private System.Windows.Forms.Button btnLookForRansomware;
    }
}

