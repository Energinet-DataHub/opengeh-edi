namespace SoapTest
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
            this.BtnPeek = new System.Windows.Forms.Button();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtMessageId = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.BtnGetMessage = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // BtnPeek
            // 
            this.BtnPeek.Location = new System.Drawing.Point(16, 18);
            this.BtnPeek.Margin = new System.Windows.Forms.Padding(2);
            this.BtnPeek.Name = "BtnPeek";
            this.BtnPeek.Size = new System.Drawing.Size(116, 61);
            this.BtnPeek.TabIndex = 0;
            this.BtnPeek.Text = "peek message";
            this.BtnPeek.UseVisualStyleBackColor = true;
            this.BtnPeek.Click += new System.EventHandler(this.BtnPeek_Click);
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(16, 164);
            this.txtResult.Margin = new System.Windows.Forms.Padding(2);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.Size = new System.Drawing.Size(1007, 368);
            this.txtResult.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 140);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Output";
            // 
            // txtMessageId
            // 
            this.txtMessageId.Location = new System.Drawing.Point(158, 49);
            this.txtMessageId.Name = "txtMessageId";
            this.txtMessageId.Size = new System.Drawing.Size(127, 20);
            this.txtMessageId.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(155, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "MessageId";
            // 
            // BtnGetMessage
            // 
            this.BtnGetMessage.Location = new System.Drawing.Point(298, 36);
            this.BtnGetMessage.Name = "BtnGetMessage";
            this.BtnGetMessage.Size = new System.Drawing.Size(92, 32);
            this.BtnGetMessage.TabIndex = 5;
            this.BtnGetMessage.Text = "Get message";
            this.BtnGetMessage.UseVisualStyleBackColor = true;
            this.BtnGetMessage.Click += new System.EventHandler(this.BtnGetMessage_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1034, 552);
            this.Controls.Add(this.BtnGetMessage);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtMessageId);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.BtnPeek);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BtnPeek;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtMessageId;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button BtnGetMessage;
    }
}

