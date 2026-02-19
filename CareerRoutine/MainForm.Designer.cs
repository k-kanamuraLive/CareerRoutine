namespace CareerRoutine
{
    partial class MainForm
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
            progressBar1 = new ProgressBar();
            btnFetch = new Button();
            lnkOpen = new LinkLabel();
            ContentaTextBox = new TextBox();
            SuspendLayout();
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(0, 0);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(799, 23);
            progressBar1.TabIndex = 0;
            // 
            // btnFetch
            // 
            btnFetch.Location = new Point(12, 29);
            btnFetch.Name = "btnFetch";
            btnFetch.Size = new Size(75, 23);
            btnFetch.TabIndex = 1;
            btnFetch.Text = "今日の1件";
            btnFetch.UseVisualStyleBackColor = true;
            btnFetch.Click += btnFetch_Click;
            // 
            // lnkOpen
            // 
            lnkOpen.AutoSize = true;
            lnkOpen.Location = new Point(93, 33);
            lnkOpen.Name = "lnkOpen";
            lnkOpen.Size = new Size(0, 15);
            lnkOpen.TabIndex = 2;
            lnkOpen.TabStop = true;
            lnkOpen.LinkClicked += lnkOpen_LinkClicked;
            // 
            // ContentaTextBox
            // 
            ContentaTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ContentaTextBox.Location = new Point(12, 58);
            ContentaTextBox.Multiline = true;
            ContentaTextBox.Name = "ContentaTextBox";
            ContentaTextBox.ReadOnly = true;
            ContentaTextBox.ScrollBars = ScrollBars.Vertical;
            ContentaTextBox.Size = new Size(776, 380);
            ContentaTextBox.TabIndex = 4;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(ContentaTextBox);
            Controls.Add(lnkOpen);
            Controls.Add(btnFetch);
            Controls.Add(progressBar1);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ProgressBar progressBar1;
        private Button btnFetch;
        private LinkLabel lnkOpen;
        private TextBox ContentaTextBox;
    }
}
