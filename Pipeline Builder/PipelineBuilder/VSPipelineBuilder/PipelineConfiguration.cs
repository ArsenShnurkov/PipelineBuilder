using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;

namespace VSPipelineBuilder
{
    public partial class PipelineConfiguration : Form
    {
        private DTE2 _root;

        public PipelineConfiguration()
        {
            InitializeComponent();
            c_projects.SelectedIndexChanged += new EventHandler(c_projects_SelectedIndexChanged);
        }

        void c_projects_SelectedIndexChanged(object sender, EventArgs e)
        {
            Project p = SourceProject;
            InitializeProject(p);
        }

        private void b_projectOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog().Equals(DialogResult.OK))
            {
                t_sourceOutput.Text = dialog.SelectedPath;
            }
            
        }

        private void b_buildOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog().Equals(DialogResult.OK))
            {
                t_binaryOutput.Text = dialog.SelectedPath;
            }
        }

        internal void Initialize(DTE2 root)
        {
            _root = root;
            t_binaryOutput.Text = "";
            t_sourceOutput.Text = "";
            Project contractProject = null;
            foreach (Project project in root.Solution.Projects)
            {
                c_projects.Items.Add(project.Name);
                if (project.Name.Contains("Contracts"))
                {
                    contractProject = project;
                }
            }
            if (contractProject != null)
            {
                c_projects.SelectedItem = contractProject.Name;
                InitializeProject(contractProject);
            }
            else
            {
                object[] selectedProjects = (object[])root.ActiveSolutionProjects;
                if (selectedProjects.Length > 0)
                {
                    Project p = (Project)selectedProjects[0];
                    c_projects.SelectedItem = p.Name;
                    InitializeProject(p);
                }
            }
        }

        private void InitializeProject(Project p)
        {
            t_sourceOutput.Text = Pop(_root.Solution.FileName);
            t_binaryOutput.Text = Pop(Pop(Connect.GetOutputAssembly(p)));
        }

        public Project SourceProject
        {
            get
            {
                if (!String.IsNullOrEmpty(c_projects.SelectedItem.ToString()))
                {
                    foreach (Project project in _root.Solution.Projects)
                    {
                        if (project.Name.Equals(c_projects.SelectedItem.ToString()))
                        {
                            return project;
                        }
                    }
                }
                return null;
            }
        }

        public string ProjectDestination
        {
            get
            {
                return t_sourceOutput.Text;
            }
        }

        public string BuildDestination
        {
            get
            {
                return t_binaryOutput.Text;
            }
        }

        private string Pop(string source)
        {
            return Connect.Pop(source);
        }
       
    }
}