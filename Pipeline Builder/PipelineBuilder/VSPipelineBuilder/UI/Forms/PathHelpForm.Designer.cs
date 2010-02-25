namespace VSPipelineBuilder.UI.Forms
{
    partial class PathHelpForm
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
            this.constantsListView = new System.Windows.Forms.ListView();
            this.constantColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.descriptionColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // constantsListView
            // 
            this.constantsListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.constantsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.constantColumnHeader,
            this.descriptionColumnHeader});
            this.constantsListView.FullRowSelect = true;
            this.constantsListView.GridLines = true;
            this.constantsListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.constantsListView.Location = new System.Drawing.Point(12, 12);
            this.constantsListView.MultiSelect = false;
            this.constantsListView.Name = "constantsListView";
            this.constantsListView.Size = new System.Drawing.Size(674, 258);
            this.constantsListView.TabIndex = 0;
            this.constantsListView.UseCompatibleStateImageBehavior = false;
            this.constantsListView.View = System.Windows.Forms.View.Details;
            // 
            // constantColumnHeader
            // 
            this.constantColumnHeader.Text = "Constant";
            this.constantColumnHeader.Width = 175;
            // 
            // descriptionColumnHeader
            // 
            this.descriptionColumnHeader.Text = "Description";
            this.descriptionColumnHeader.Width = 479;
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(611, 276);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 1;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // PathHelpForm
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(698, 311);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.constantsListView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PathHelpForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Path help";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView constantsListView;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ColumnHeader constantColumnHeader;
        private System.Windows.Forms.ColumnHeader descriptionColumnHeader;
    }
}