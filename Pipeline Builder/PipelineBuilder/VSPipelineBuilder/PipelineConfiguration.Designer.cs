namespace VSPipelineBuilder
{
    partial class PipelineConfiguration
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
			System.Windows.Forms.Label lblContractSourceProject;
			System.Windows.Forms.Label lblProjectLocation;
			System.Windows.Forms.Label lblBuildLocation;
			this.b_ok = new System.Windows.Forms.Button();
			this.b_cancel = new System.Windows.Forms.Button();
			this.cbContractProject = new System.Windows.Forms.ComboBox();
			this.txtProjectLocation = new System.Windows.Forms.TextBox();
			this.btnProjectLocation = new System.Windows.Forms.Button();
			this.btnBuildOutput = new System.Windows.Forms.Button();
			this.txtBinaryOutput = new System.Windows.Forms.TextBox();
			this.lblAddInSideAdapter = new System.Windows.Forms.Label();
			this.cbAddInSideAdapter = new System.Windows.Forms.ComboBox();
			this.lblAddInView = new System.Windows.Forms.Label();
			this.lblHostView = new System.Windows.Forms.Label();
			this.cbAddInView = new System.Windows.Forms.ComboBox();
			this.cbHostView = new System.Windows.Forms.ComboBox();
			this.cbHostSideAdapter = new System.Windows.Forms.ComboBox();
			this.lblHostSideAdapter = new System.Windows.Forms.Label();
			this.cbSingleViewProject = new System.Windows.Forms.CheckBox();
			this.btnGuessProjects = new System.Windows.Forms.Button();
			lblContractSourceProject = new System.Windows.Forms.Label();
			lblProjectLocation = new System.Windows.Forms.Label();
			lblBuildLocation = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// lblContractSourceProject
			// 
			lblContractSourceProject.AutoSize = true;
			lblContractSourceProject.Location = new System.Drawing.Point(9, 19);
			lblContractSourceProject.Name = "lblContractSourceProject";
			lblContractSourceProject.Size = new System.Drawing.Size(120, 13);
			lblContractSourceProject.TabIndex = 3;
			lblContractSourceProject.Text = "Contract Source Project";
			// 
			// lblProjectLocation
			// 
			lblProjectLocation.AutoSize = true;
			lblProjectLocation.Location = new System.Drawing.Point(9, 65);
			lblProjectLocation.Name = "lblProjectLocation";
			lblProjectLocation.Size = new System.Drawing.Size(84, 13);
			lblProjectLocation.TabIndex = 6;
			lblProjectLocation.Text = "Project Location";
			// 
			// lblBuildLocation
			// 
			lblBuildLocation.AutoSize = true;
			lblBuildLocation.Location = new System.Drawing.Point(9, 112);
			lblBuildLocation.Name = "lblBuildLocation";
			lblBuildLocation.Size = new System.Drawing.Size(74, 13);
			lblBuildLocation.TabIndex = 9;
			lblBuildLocation.Text = "Build Location";
			// 
			// b_ok
			// 
			this.b_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.b_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.b_ok.Location = new System.Drawing.Point(286, 378);
			this.b_ok.Name = "b_ok";
			this.b_ok.Size = new System.Drawing.Size(75, 23);
			this.b_ok.TabIndex = 0;
			this.b_ok.Text = "OK";
			this.b_ok.UseVisualStyleBackColor = true;
			// 
			// b_cancel
			// 
			this.b_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.b_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.b_cancel.Location = new System.Drawing.Point(367, 378);
			this.b_cancel.Name = "b_cancel";
			this.b_cancel.Size = new System.Drawing.Size(75, 23);
			this.b_cancel.TabIndex = 1;
			this.b_cancel.Text = "Cancel";
			this.b_cancel.UseVisualStyleBackColor = true;
			// 
			// cbContractProject
			// 
			this.cbContractProject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cbContractProject.FormattingEnabled = true;
			this.cbContractProject.Location = new System.Drawing.Point(12, 35);
			this.cbContractProject.Name = "cbContractProject";
			this.cbContractProject.Size = new System.Drawing.Size(428, 21);
			this.cbContractProject.TabIndex = 2;
			this.cbContractProject.SelectedValueChanged += new System.EventHandler(this.cbContractProject_SelectedValueChanged);
			// 
			// txtProjectLocation
			// 
			this.txtProjectLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtProjectLocation.Location = new System.Drawing.Point(12, 81);
			this.txtProjectLocation.Name = "txtProjectLocation";
			this.txtProjectLocation.Size = new System.Drawing.Size(347, 20);
			this.txtProjectLocation.TabIndex = 4;
			// 
			// btnProjectLocation
			// 
			this.btnProjectLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnProjectLocation.Location = new System.Drawing.Point(367, 79);
			this.btnProjectLocation.Name = "btnProjectLocation";
			this.btnProjectLocation.Size = new System.Drawing.Size(75, 23);
			this.btnProjectLocation.TabIndex = 5;
			this.btnProjectLocation.Text = "Browse";
			this.btnProjectLocation.UseVisualStyleBackColor = true;
			this.btnProjectLocation.Click += new System.EventHandler(this.btnProjectOutput_Click);
			// 
			// btnBuildOutput
			// 
			this.btnBuildOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBuildOutput.Location = new System.Drawing.Point(367, 126);
			this.btnBuildOutput.Name = "btnBuildOutput";
			this.btnBuildOutput.Size = new System.Drawing.Size(75, 23);
			this.btnBuildOutput.TabIndex = 8;
			this.btnBuildOutput.Text = "Browse";
			this.btnBuildOutput.UseVisualStyleBackColor = true;
			this.btnBuildOutput.Click += new System.EventHandler(this.btnOutput_Click);
			// 
			// txtBinaryOutput
			// 
			this.txtBinaryOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtBinaryOutput.Location = new System.Drawing.Point(12, 128);
			this.txtBinaryOutput.Name = "txtBinaryOutput";
			this.txtBinaryOutput.Size = new System.Drawing.Size(348, 20);
			this.txtBinaryOutput.TabIndex = 7;
			// 
			// lblAddInSideAdapter
			// 
			this.lblAddInSideAdapter.AutoSize = true;
			this.lblAddInSideAdapter.Location = new System.Drawing.Point(9, 169);
			this.lblAddInSideAdapter.Name = "lblAddInSideAdapter";
			this.lblAddInSideAdapter.Size = new System.Drawing.Size(138, 13);
			this.lblAddInSideAdapter.TabIndex = 10;
			this.lblAddInSideAdapter.Text = "Add-In Side Adapter Project";
			// 
			// cbAddInSideAdapter
			// 
			this.cbAddInSideAdapter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cbAddInSideAdapter.FormattingEnabled = true;
			this.cbAddInSideAdapter.Location = new System.Drawing.Point(12, 185);
			this.cbAddInSideAdapter.Name = "cbAddInSideAdapter";
			this.cbAddInSideAdapter.Size = new System.Drawing.Size(428, 21);
			this.cbAddInSideAdapter.TabIndex = 2;
			this.cbAddInSideAdapter.SelectedIndexChanged += new System.EventHandler(this.cbContractProject_SelectedValueChanged);
			// 
			// lblAddInView
			// 
			this.lblAddInView.AutoSize = true;
			this.lblAddInView.Location = new System.Drawing.Point(9, 282);
			this.lblAddInView.Name = "lblAddInView";
			this.lblAddInView.Size = new System.Drawing.Size(194, 13);
			this.lblAddInView.TabIndex = 10;
			this.lblAddInView.Text = "Add-In View (as seen from Host) Project";
			// 
			// lblHostView
			// 
			this.lblHostView.AutoSize = true;
			this.lblHostView.Location = new System.Drawing.Point(9, 329);
			this.lblHostView.Name = "lblHostView";
			this.lblHostView.Size = new System.Drawing.Size(194, 13);
			this.lblHostView.TabIndex = 10;
			this.lblHostView.Text = "Host View (as seen from Add-In) Project";
			// 
			// cbAddInView
			// 
			this.cbAddInView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cbAddInView.FormattingEnabled = true;
			this.cbAddInView.Location = new System.Drawing.Point(12, 298);
			this.cbAddInView.Name = "cbAddInView";
			this.cbAddInView.Size = new System.Drawing.Size(428, 21);
			this.cbAddInView.TabIndex = 2;
			this.cbAddInView.SelectedIndexChanged += new System.EventHandler(this.cbContractProject_SelectedValueChanged);
			// 
			// cbHostView
			// 
			this.cbHostView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cbHostView.FormattingEnabled = true;
			this.cbHostView.Location = new System.Drawing.Point(12, 345);
			this.cbHostView.Name = "cbHostView";
			this.cbHostView.Size = new System.Drawing.Size(428, 21);
			this.cbHostView.TabIndex = 2;
			this.cbHostView.SelectedIndexChanged += new System.EventHandler(this.cbContractProject_SelectedValueChanged);
			// 
			// cbHostSideAdapter
			// 
			this.cbHostSideAdapter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.cbHostSideAdapter.FormattingEnabled = true;
			this.cbHostSideAdapter.Location = new System.Drawing.Point(12, 227);
			this.cbHostSideAdapter.Name = "cbHostSideAdapter";
			this.cbHostSideAdapter.Size = new System.Drawing.Size(428, 21);
			this.cbHostSideAdapter.TabIndex = 2;
			this.cbHostSideAdapter.SelectedIndexChanged += new System.EventHandler(this.cbContractProject_SelectedValueChanged);
			// 
			// lblHostSideAdapter
			// 
			this.lblHostSideAdapter.AutoSize = true;
			this.lblHostSideAdapter.Location = new System.Drawing.Point(9, 209);
			this.lblHostSideAdapter.Name = "lblHostSideAdapter";
			this.lblHostSideAdapter.Size = new System.Drawing.Size(129, 13);
			this.lblHostSideAdapter.TabIndex = 10;
			this.lblHostSideAdapter.Text = "Host Side Adapter Project";
			// 
			// cbSingleViewProject
			// 
			this.cbSingleViewProject.AutoSize = true;
			this.cbSingleViewProject.Location = new System.Drawing.Point(12, 262);
			this.cbSingleViewProject.Name = "cbSingleViewProject";
			this.cbSingleViewProject.Size = new System.Drawing.Size(117, 17);
			this.cbSingleViewProject.TabIndex = 11;
			this.cbSingleViewProject.Text = "Single View Project";
			this.cbSingleViewProject.UseVisualStyleBackColor = true;
			// 
			// btnGuessProjects
			// 
			this.btnGuessProjects.Location = new System.Drawing.Point(157, 378);
			this.btnGuessProjects.Name = "btnGuessProjects";
			this.btnGuessProjects.Size = new System.Drawing.Size(123, 23);
			this.btnGuessProjects.TabIndex = 12;
			this.btnGuessProjects.Text = "Guess projects";
			this.btnGuessProjects.UseVisualStyleBackColor = true;
			this.btnGuessProjects.Click += new System.EventHandler(this.btnGuessProjects_Click);
			// 
			// PipelineConfiguration
			// 
			this.AcceptButton = this.b_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.CancelButton = this.b_cancel;
			this.ClientSize = new System.Drawing.Size(454, 407);
			this.Controls.Add(this.btnGuessProjects);
			this.Controls.Add(this.cbSingleViewProject);
			this.Controls.Add(this.lblHostView);
			this.Controls.Add(this.lblAddInView);
			this.Controls.Add(this.lblHostSideAdapter);
			this.Controls.Add(this.lblAddInSideAdapter);
			this.Controls.Add(lblBuildLocation);
			this.Controls.Add(this.btnBuildOutput);
			this.Controls.Add(this.txtBinaryOutput);
			this.Controls.Add(lblProjectLocation);
			this.Controls.Add(this.btnProjectLocation);
			this.Controls.Add(this.txtProjectLocation);
			this.Controls.Add(lblContractSourceProject);
			this.Controls.Add(this.cbHostView);
			this.Controls.Add(this.cbAddInView);
			this.Controls.Add(this.cbHostSideAdapter);
			this.Controls.Add(this.cbAddInSideAdapter);
			this.Controls.Add(this.cbContractProject);
			this.Controls.Add(this.b_cancel);
			this.Controls.Add(this.b_ok);
			this.MinimumSize = new System.Drawing.Size(441, 235);
			this.Name = "PipelineConfiguration";
			this.Text = "PipelineConfiguration";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button b_ok;
        private System.Windows.Forms.Button b_cancel;
        private System.Windows.Forms.ComboBox cbContractProject;
        private System.Windows.Forms.TextBox txtProjectLocation;
        private System.Windows.Forms.Button btnProjectLocation;
        private System.Windows.Forms.Button btnBuildOutput;
        private System.Windows.Forms.TextBox txtBinaryOutput;
		private System.Windows.Forms.Label lblAddInSideAdapter;
		private System.Windows.Forms.ComboBox cbAddInSideAdapter;
		private System.Windows.Forms.Label lblAddInView;
		private System.Windows.Forms.Label lblHostView;
		private System.Windows.Forms.ComboBox cbAddInView;
		private System.Windows.Forms.ComboBox cbHostView;
		private System.Windows.Forms.ComboBox cbHostSideAdapter;
		private System.Windows.Forms.Label lblHostSideAdapter;
		private System.Windows.Forms.CheckBox cbSingleViewProject;
		private System.Windows.Forms.Button btnGuessProjects;
    }
}