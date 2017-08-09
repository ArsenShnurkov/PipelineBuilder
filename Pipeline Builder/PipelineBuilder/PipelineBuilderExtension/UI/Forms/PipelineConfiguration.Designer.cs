namespace PipelineBuilderExtension.UI.Forms
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
            System.Windows.Forms.Label lblProjectLocation;
            System.Windows.Forms.Label lblContractSourceProject;
            System.Windows.Forms.Label lblBuildLocation;
            this.okButton = new System.Windows.Forms.Button();
            this.b_cancel = new System.Windows.Forms.Button();
            this.btnGuessProjects = new System.Windows.Forms.Button();
            this.pipelineConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.cbSingleViewProject = new System.Windows.Forms.CheckBox();
            this.lblHostView = new System.Windows.Forms.Label();
            this.lblAddInView = new System.Windows.Forms.Label();
            this.lblHostSideAdapter = new System.Windows.Forms.Label();
            this.lblAddInSideAdapter = new System.Windows.Forms.Label();
            this.cbHostView = new System.Windows.Forms.ComboBox();
            this.cbAddInView = new System.Windows.Forms.ComboBox();
            this.cbHostSideAdapter = new System.Windows.Forms.ComboBox();
            this.cbAddInSideAdapter = new System.Windows.Forms.ComboBox();
            this.sourceConfigurationGroupBox = new System.Windows.Forms.GroupBox();
            this.btnProjectLocation = new System.Windows.Forms.Button();
            this.txtProjectLocation = new System.Windows.Forms.TextBox();
            this.cbContractProject = new System.Windows.Forms.ComboBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pathHelpButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnBuildOutput = new System.Windows.Forms.Button();
            this.txtBinaryOutput = new System.Windows.Forms.TextBox();
            this.stepNumber3 = new PipelineBuilderExtension.UI.Controls.StepNumber();
            this.stepNumber2 = new PipelineBuilderExtension.UI.Controls.StepNumber();
            this.stepNumber1 = new PipelineBuilderExtension.UI.Controls.StepNumber();
            this.rememberSettingsCheckBox = new System.Windows.Forms.CheckBox();
            lblProjectLocation = new System.Windows.Forms.Label();
            lblContractSourceProject = new System.Windows.Forms.Label();
            lblBuildLocation = new System.Windows.Forms.Label();
            this.pipelineConfigurationGroupBox.SuspendLayout();
            this.sourceConfigurationGroupBox.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblProjectLocation
            // 
            lblProjectLocation.AutoSize = true;
            lblProjectLocation.Location = new System.Drawing.Point(6, 71);
            lblProjectLocation.Name = "lblProjectLocation";
            lblProjectLocation.Size = new System.Drawing.Size(84, 13);
            lblProjectLocation.TabIndex = 11;
            lblProjectLocation.Text = "Project Location";
            // 
            // lblContractSourceProject
            // 
            lblContractSourceProject.AutoSize = true;
            lblContractSourceProject.Location = new System.Drawing.Point(6, 25);
            lblContractSourceProject.Name = "lblContractSourceProject";
            lblContractSourceProject.Size = new System.Drawing.Size(120, 13);
            lblContractSourceProject.TabIndex = 8;
            lblContractSourceProject.Text = "Contract Source Project";
            // 
            // lblBuildLocation
            // 
            lblBuildLocation.AutoSize = true;
            lblBuildLocation.Location = new System.Drawing.Point(6, 25);
            lblBuildLocation.Name = "lblBuildLocation";
            lblBuildLocation.Size = new System.Drawing.Size(74, 13);
            lblBuildLocation.TabIndex = 12;
            lblBuildLocation.Text = "Build Location";
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(339, 672);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 7;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // b_cancel
            // 
            this.b_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.b_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.b_cancel.Location = new System.Drawing.Point(420, 672);
            this.b_cancel.Name = "b_cancel";
            this.b_cancel.Size = new System.Drawing.Size(75, 23);
            this.b_cancel.TabIndex = 8;
            this.b_cancel.Text = "Cancel";
            this.b_cancel.UseVisualStyleBackColor = true;
            // 
            // btnGuessProjects
            // 
            this.btnGuessProjects.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGuessProjects.Location = new System.Drawing.Point(354, 228);
            this.btnGuessProjects.Name = "btnGuessProjects";
            this.btnGuessProjects.Size = new System.Drawing.Size(123, 23);
            this.btnGuessProjects.TabIndex = 5;
            this.btnGuessProjects.Text = "Guess projects";
            this.btnGuessProjects.UseVisualStyleBackColor = true;
            this.btnGuessProjects.Click += new System.EventHandler(this.btnGuessProjects_Click);
            // 
            // pipelineConfigurationGroupBox
            // 
            this.pipelineConfigurationGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pipelineConfigurationGroupBox.Controls.Add(this.cbSingleViewProject);
            this.pipelineConfigurationGroupBox.Controls.Add(this.btnGuessProjects);
            this.pipelineConfigurationGroupBox.Controls.Add(this.lblHostView);
            this.pipelineConfigurationGroupBox.Controls.Add(this.lblAddInView);
            this.pipelineConfigurationGroupBox.Controls.Add(this.lblHostSideAdapter);
            this.pipelineConfigurationGroupBox.Controls.Add(this.lblAddInSideAdapter);
            this.pipelineConfigurationGroupBox.Controls.Add(this.cbHostView);
            this.pipelineConfigurationGroupBox.Controls.Add(this.cbAddInView);
            this.pipelineConfigurationGroupBox.Controls.Add(this.cbHostSideAdapter);
            this.pipelineConfigurationGroupBox.Controls.Add(this.cbAddInSideAdapter);
            this.pipelineConfigurationGroupBox.Location = new System.Drawing.Point(12, 231);
            this.pipelineConfigurationGroupBox.Name = "pipelineConfigurationGroupBox";
            this.pipelineConfigurationGroupBox.Size = new System.Drawing.Size(483, 259);
            this.pipelineConfigurationGroupBox.TabIndex = 3;
            this.pipelineConfigurationGroupBox.TabStop = false;
            this.pipelineConfigurationGroupBox.Text = "Pipeline configuration";
            // 
            // cbSingleViewProject
            // 
            this.cbSingleViewProject.AutoSize = true;
            this.cbSingleViewProject.Location = new System.Drawing.Point(9, 118);
            this.cbSingleViewProject.Name = "cbSingleViewProject";
            this.cbSingleViewProject.Size = new System.Drawing.Size(117, 17);
            this.cbSingleViewProject.TabIndex = 2;
            this.cbSingleViewProject.Text = "Single View Project";
            this.cbSingleViewProject.UseVisualStyleBackColor = true;
            this.cbSingleViewProject.CheckedChanged += new System.EventHandler(this.singleViewCheckbox_Changed);
            // 
            // lblHostView
            // 
            this.lblHostView.AutoSize = true;
            this.lblHostView.Location = new System.Drawing.Point(6, 185);
            this.lblHostView.Name = "lblHostView";
            this.lblHostView.Size = new System.Drawing.Size(194, 13);
            this.lblHostView.TabIndex = 17;
            this.lblHostView.Text = "Host View (as seen from Add-In) Project";
            // 
            // lblAddInView
            // 
            this.lblAddInView.AutoSize = true;
            this.lblAddInView.Location = new System.Drawing.Point(6, 138);
            this.lblAddInView.Name = "lblAddInView";
            this.lblAddInView.Size = new System.Drawing.Size(194, 13);
            this.lblAddInView.TabIndex = 18;
            this.lblAddInView.Text = "Add-In View (as seen from Host) Project";
            // 
            // lblHostSideAdapter
            // 
            this.lblHostSideAdapter.AutoSize = true;
            this.lblHostSideAdapter.Location = new System.Drawing.Point(6, 65);
            this.lblHostSideAdapter.Name = "lblHostSideAdapter";
            this.lblHostSideAdapter.Size = new System.Drawing.Size(129, 13);
            this.lblHostSideAdapter.TabIndex = 19;
            this.lblHostSideAdapter.Text = "Host Side Adapter Project";
            // 
            // lblAddInSideAdapter
            // 
            this.lblAddInSideAdapter.AutoSize = true;
            this.lblAddInSideAdapter.Location = new System.Drawing.Point(6, 25);
            this.lblAddInSideAdapter.Name = "lblAddInSideAdapter";
            this.lblAddInSideAdapter.Size = new System.Drawing.Size(138, 13);
            this.lblAddInSideAdapter.TabIndex = 16;
            this.lblAddInSideAdapter.Text = "Add-In Side Adapter Project";
            // 
            // cbHostView
            // 
            this.cbHostView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbHostView.FormattingEnabled = true;
            this.cbHostView.Location = new System.Drawing.Point(9, 201);
            this.cbHostView.Name = "cbHostView";
            this.cbHostView.Size = new System.Drawing.Size(468, 21);
            this.cbHostView.TabIndex = 4;
            // 
            // cbAddInView
            // 
            this.cbAddInView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAddInView.FormattingEnabled = true;
            this.cbAddInView.Location = new System.Drawing.Point(9, 154);
            this.cbAddInView.Name = "cbAddInView";
            this.cbAddInView.Size = new System.Drawing.Size(468, 21);
            this.cbAddInView.TabIndex = 3;
            // 
            // cbHostSideAdapter
            // 
            this.cbHostSideAdapter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbHostSideAdapter.FormattingEnabled = true;
            this.cbHostSideAdapter.Location = new System.Drawing.Point(9, 83);
            this.cbHostSideAdapter.Name = "cbHostSideAdapter";
            this.cbHostSideAdapter.Size = new System.Drawing.Size(468, 21);
            this.cbHostSideAdapter.TabIndex = 1;
            // 
            // cbAddInSideAdapter
            // 
            this.cbAddInSideAdapter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAddInSideAdapter.FormattingEnabled = true;
            this.cbAddInSideAdapter.Location = new System.Drawing.Point(9, 41);
            this.cbAddInSideAdapter.Name = "cbAddInSideAdapter";
            this.cbAddInSideAdapter.Size = new System.Drawing.Size(468, 21);
            this.cbAddInSideAdapter.TabIndex = 0;
            // 
            // sourceConfigurationGroupBox
            // 
            this.sourceConfigurationGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceConfigurationGroupBox.Controls.Add(lblProjectLocation);
            this.sourceConfigurationGroupBox.Controls.Add(this.btnProjectLocation);
            this.sourceConfigurationGroupBox.Controls.Add(this.txtProjectLocation);
            this.sourceConfigurationGroupBox.Controls.Add(lblContractSourceProject);
            this.sourceConfigurationGroupBox.Controls.Add(this.cbContractProject);
            this.sourceConfigurationGroupBox.Location = new System.Drawing.Point(12, 51);
            this.sourceConfigurationGroupBox.Name = "sourceConfigurationGroupBox";
            this.sourceConfigurationGroupBox.Size = new System.Drawing.Size(483, 126);
            this.sourceConfigurationGroupBox.TabIndex = 1;
            this.sourceConfigurationGroupBox.TabStop = false;
            this.sourceConfigurationGroupBox.Text = "Source configuration";
            // 
            // btnProjectLocation
            // 
            this.btnProjectLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnProjectLocation.Location = new System.Drawing.Point(452, 84);
            this.btnProjectLocation.Name = "btnProjectLocation";
            this.btnProjectLocation.Size = new System.Drawing.Size(25, 25);
            this.btnProjectLocation.TabIndex = 2;
            this.btnProjectLocation.Text = "...";
            this.btnProjectLocation.UseVisualStyleBackColor = true;
            this.btnProjectLocation.Click += new System.EventHandler(this.btnProjectOutput_Click);
            // 
            // txtProjectLocation
            // 
            this.txtProjectLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProjectLocation.Location = new System.Drawing.Point(9, 87);
            this.txtProjectLocation.Name = "txtProjectLocation";
            this.txtProjectLocation.Size = new System.Drawing.Size(437, 20);
            this.txtProjectLocation.TabIndex = 1;
            // 
            // cbContractProject
            // 
            this.cbContractProject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbContractProject.FormattingEnabled = true;
            this.cbContractProject.Location = new System.Drawing.Point(9, 41);
            this.cbContractProject.Name = "cbContractProject";
            this.cbContractProject.Size = new System.Drawing.Size(468, 21);
            this.cbContractProject.TabIndex = 0;
            this.cbContractProject.Click += new System.EventHandler(this.cbContractProject_SelectedValueChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pathHelpButton);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(lblBuildLocation);
            this.groupBox2.Controls.Add(this.btnBuildOutput);
            this.groupBox2.Controls.Add(this.txtBinaryOutput);
            this.groupBox2.Location = new System.Drawing.Point(12, 544);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(483, 119);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Output configuration";
            // 
            // pathHelpButton
            // 
            this.pathHelpButton.Image = global::PipelineBuilderExtension.Properties.Resources.Help;
            this.pathHelpButton.Location = new System.Drawing.Point(452, 38);
            this.pathHelpButton.Name = "pathHelpButton";
            this.pathHelpButton.Size = new System.Drawing.Size(25, 25);
            this.pathHelpButton.TabIndex = 2;
            this.pathHelpButton.UseVisualStyleBackColor = true;
            this.pathHelpButton.Click += new System.EventHandler(this.pathHelpButton_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(9, 84);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(468, 32);
            this.label1.TabIndex = 13;
            this.label1.Text = "Note: did you know that it is possible to use constants in the output path? Use t" +
                "he help button to see what constants are possible.";
            // 
            // btnBuildOutput
            // 
            this.btnBuildOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBuildOutput.Location = new System.Drawing.Point(421, 38);
            this.btnBuildOutput.Name = "btnBuildOutput";
            this.btnBuildOutput.Size = new System.Drawing.Size(25, 25);
            this.btnBuildOutput.TabIndex = 1;
            this.btnBuildOutput.Text = "...";
            this.btnBuildOutput.UseVisualStyleBackColor = true;
            this.btnBuildOutput.Click += new System.EventHandler(this.btnOutput_Click);
            // 
            // txtBinaryOutput
            // 
            this.txtBinaryOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBinaryOutput.Location = new System.Drawing.Point(9, 41);
            this.txtBinaryOutput.Name = "txtBinaryOutput";
            this.txtBinaryOutput.Size = new System.Drawing.Size(406, 20);
            this.txtBinaryOutput.TabIndex = 0;
            // 
            // stepNumber3
            // 
            this.stepNumber3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.stepNumber3.BorderColor = System.Drawing.Color.DimGray;
            this.stepNumber3.BrushColor = System.Drawing.Color.Silver;
            this.stepNumber3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.stepNumber3.HelpText = "Specify the output options";
            this.stepNumber3.Location = new System.Drawing.Point(11, 505);
            this.stepNumber3.Name = "stepNumber3";
            this.stepNumber3.Size = new System.Drawing.Size(484, 33);
            this.stepNumber3.TabIndex = 4;
            this.stepNumber3.TextAlignStyle = PipelineBuilderExtension.UI.Controls.StepNumber.TextAlignStyleType.MiddleLeft;
            this.stepNumber3.TextColor = System.Drawing.Color.Black;
            // 
            // stepNumber2
            // 
            this.stepNumber2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.stepNumber2.BorderColor = System.Drawing.Color.DimGray;
            this.stepNumber2.BrushColor = System.Drawing.Color.Silver;
            this.stepNumber2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.stepNumber2.HelpText = "Specify the pipeline options";
            this.stepNumber2.Location = new System.Drawing.Point(15, 192);
            this.stepNumber2.Name = "stepNumber2";
            this.stepNumber2.Size = new System.Drawing.Size(480, 33);
            this.stepNumber2.TabIndex = 2;
            this.stepNumber2.TextAlignStyle = PipelineBuilderExtension.UI.Controls.StepNumber.TextAlignStyleType.MiddleLeft;
            this.stepNumber2.TextColor = System.Drawing.Color.Black;
            // 
            // stepNumber1
            // 
            this.stepNumber1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.stepNumber1.BorderColor = System.Drawing.Color.DimGray;
            this.stepNumber1.BrushColor = System.Drawing.Color.Silver;
            this.stepNumber1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.stepNumber1.HelpText = "Specify the source project";
            this.stepNumber1.Location = new System.Drawing.Point(12, 12);
            this.stepNumber1.Name = "stepNumber1";
            this.stepNumber1.Size = new System.Drawing.Size(483, 33);
            this.stepNumber1.TabIndex = 0;
            this.stepNumber1.TextAlignStyle = PipelineBuilderExtension.UI.Controls.StepNumber.TextAlignStyleType.MiddleLeft;
            this.stepNumber1.TextColor = System.Drawing.Color.Black;
            // 
            // rememberSettingsCheckBox
            // 
            this.rememberSettingsCheckBox.AutoSize = true;
            this.rememberSettingsCheckBox.Location = new System.Drawing.Point(12, 676);
            this.rememberSettingsCheckBox.Name = "rememberSettingsCheckBox";
            this.rememberSettingsCheckBox.Size = new System.Drawing.Size(189, 17);
            this.rememberSettingsCheckBox.TabIndex = 6;
            this.rememberSettingsCheckBox.Text = "Remember values for the next time";
            this.rememberSettingsCheckBox.UseVisualStyleBackColor = true;
            // 
            // PipelineConfiguration
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.CancelButton = this.b_cancel;
            this.ClientSize = new System.Drawing.Size(507, 701);
            this.Controls.Add(this.rememberSettingsCheckBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.stepNumber3);
            this.Controls.Add(this.stepNumber2);
            this.Controls.Add(this.stepNumber1);
            this.Controls.Add(this.sourceConfigurationGroupBox);
            this.Controls.Add(this.pipelineConfigurationGroupBox);
            this.Controls.Add(this.b_cancel);
            this.Controls.Add(this.okButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(441, 235);
            this.Name = "PipelineConfiguration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Pipeline Configuration";
            this.pipelineConfigurationGroupBox.ResumeLayout(false);
            this.pipelineConfigurationGroupBox.PerformLayout();
            this.sourceConfigurationGroupBox.ResumeLayout(false);
            this.sourceConfigurationGroupBox.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button b_cancel;
		private System.Windows.Forms.Button btnGuessProjects;
        private System.Windows.Forms.GroupBox pipelineConfigurationGroupBox;
        private System.Windows.Forms.CheckBox cbSingleViewProject;
        private System.Windows.Forms.Label lblHostView;
        private System.Windows.Forms.Label lblAddInView;
        private System.Windows.Forms.Label lblHostSideAdapter;
        private System.Windows.Forms.Label lblAddInSideAdapter;
        private System.Windows.Forms.ComboBox cbHostView;
        private System.Windows.Forms.ComboBox cbAddInView;
        private System.Windows.Forms.ComboBox cbHostSideAdapter;
        private System.Windows.Forms.ComboBox cbAddInSideAdapter;
        private System.Windows.Forms.GroupBox sourceConfigurationGroupBox;
        private PipelineBuilderExtension.UI.Controls.StepNumber stepNumber1;
        private System.Windows.Forms.Button btnProjectLocation;
        private System.Windows.Forms.TextBox txtProjectLocation;
        private System.Windows.Forms.ComboBox cbContractProject;
        private PipelineBuilderExtension.UI.Controls.StepNumber stepNumber2;
        private PipelineBuilderExtension.UI.Controls.StepNumber stepNumber3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnBuildOutput;
        private System.Windows.Forms.TextBox txtBinaryOutput;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button pathHelpButton;
        private System.Windows.Forms.CheckBox rememberSettingsCheckBox;
    }
}