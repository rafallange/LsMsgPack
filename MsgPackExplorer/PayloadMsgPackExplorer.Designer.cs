namespace MsgPackExplorer {
	partial class PayloadMsgPackExplorer {
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
				msgPackExplorer1.Dispose();
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
			this.msgPackExplorer1 = new LsMsgPackExplorer();
			this.SuspendLayout();
			// 
			// PayloadMsgPackExplorer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.msgPackExplorer1);
			this.Name = "PayloadMsgPackExplorer";
			this.Text = "Form1";
			this.ResumeLayout(false);
			// 
			// msgPackExplorer1
			// 
			this.msgPackExplorer1.ContinueOnError = false;
			this.msgPackExplorer1.Data = null;
			this.msgPackExplorer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.msgPackExplorer1.Item = null;
			this.msgPackExplorer1.Location = new System.Drawing.Point(0, 25);
			this.msgPackExplorer1.Name = "msgPackExplorer1";
			this.msgPackExplorer1.Size = new System.Drawing.Size(929, 470);
			this.msgPackExplorer1.TabIndex = 0;
		}

		#endregion

		private LsMsgPackExplorer msgPackExplorer1;
	}
}