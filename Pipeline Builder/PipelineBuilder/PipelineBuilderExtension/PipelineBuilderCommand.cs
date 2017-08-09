//------------------------------------------------------------------------------
// <copyright file="PipelineBuilder.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using PipelineBuilderExtension.UI.Forms;
using System.Reflection;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using PipelineBuilder;
using VSLangProj80;
using System.AddIn.Contract;
using PipelineBuilder.Data;

namespace PipelineBuilderExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PipelineBuilderCommand
    {
        private const string PIPELINE_BUILDER_NAME = "VSPipelineBuilder.Connect.PipelineBuilder";

        private const string GENERETED_FILES_DIRECTORY = "Generated Files";

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("78f92254-f87e-41e4-b0a5-7396b3573f24");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Exposes the DTE for use in other areas of the package.
        /// </summary>
        private readonly DTE2 dte;

        /// <summary>
        /// Exposes an instance of the pipeline configuration form.
        /// </summary>
        private readonly PipelineConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineBuilderCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PipelineBuilderCommand(Package package, DTE2 dte)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }

            this.package = package;
            this.dte = dte;
            this.config = new PipelineConfiguration();

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PipelineBuilderCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package, DTE2 dte)
        {
            Instance = new PipelineBuilderCommand(package, dte);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            EnvDTE.StatusBar statusBar = null;
            try
            {
                var config = new PipelineConfiguration();
                config.Initialize(dte);

                if (!config.ShowDialog().Equals(DialogResult.OK)) return;

                statusBar = config.SourceProject.DTE.StatusBar;
                buildPipeline(config, (curr, total) => /* update lambda */
                {
                    if (curr != total) statusBar.Progress(true, "Generating Pipeline", curr, total);
                    else statusBar.Progress(false, "Pipeline Generation Complete", 1, 1);
                });
            }
            catch (Exception exception)
            {
                showException(exception);

                if (statusBar != null)
                {
                    statusBar.Clear();
                    statusBar.Progress(false, "Problem generating pipeline.", 1, 1);
                }

            }
        }

        /// <summary>
        /// Builds the pipeline.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="reportProgress">The report progress.</param>
        private void buildPipeline(PipelineConfiguration config,
            /* curr -> total -> unit */ Action<int, int> reportProgress)
        {
            var sourceAssemblyPath = config.SourceProject.GetOutputAssembly();

            if (!File.Exists(sourceAssemblyPath))
                throw new InvalidOperationException("Please build the contract project before attempting to generate a pipeline.");

            List<PipelineSegmentSource> sources = buildSource(sourceAssemblyPath);

            Project hostAddInView = null,
                hostSideAdapterProject = null,
                addInSideAdapterProject = null,
                addInView = null, view = null;

            var sourceProject = config.SourceProject;
            var destPath = config.ProjectDestination;
            var outputPath = config.BuildDestination;

            int progress = 0;
            reportProgress(progress, sources.Count);

            foreach (var pipelineSegmentSource in sources)
            {
                // Create add-in project
                Project project = createAddInProject(destPath, pipelineSegmentSource, config);

                // if this returns null, then we have cancelled from the createAddInProject.
                // also create new add in project.
                if (project == null) return;

                // Set output path
                setOutputPath(project, pipelineSegmentSource.Type, outputPath);

                switch (pipelineSegmentSource.Type)
                {
                    case SegmentType.HostAddInView:
                        hostAddInView = project;
                        break;
                    case SegmentType.HostSideAdapter:
                        hostSideAdapterProject = project;
                        break;
                    case SegmentType.AddInSideAdapter:
                        addInSideAdapterProject = project;
                        break;
                    case SegmentType.AddInView:
                        addInView = project;
                        break;
                    case SegmentType.View:
                        view = project;
                        break;
                }

                reportProgress(progress++, sources.Count);
            }

            assertAdaptersNotNull(hostSideAdapterProject, addInSideAdapterProject);

            var hostSideAdapter2 = (VSProject2)hostSideAdapterProject.Object;
            var addInSideAdapter2 = (VSProject2)addInSideAdapterProject.Object;

            // why copy the reference twice?
            addContractReference(hostSideAdapter2, sourceProject);
            addContractReference(addInSideAdapter2, sourceProject);

            if (view == null) // both add-in-side adapters and host-side adapters.
            {
                addProjectReference(hostSideAdapter2, hostAddInView);
                addProjectReference(addInSideAdapter2, addInView);
            }
            else
            {
                addProjectReference(hostSideAdapter2, view);
                addProjectReference(addInSideAdapter2, view);
            }

            reportProgress(1, 1);
            var dte2 = (DTE2)sourceProject.DTE;

            foreach (UIHierarchyItem solution in dte2.ToolWindows.SolutionExplorer.UIHierarchyItems)
                collapseFolder(hostSideAdapter2, addInSideAdapter2, solution);
        }

        /// <summary>
        /// Adds the contract reference.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="sourceProject">The source project.</param>
		private static void addContractReference(VSProject2 target, Project sourceProject)
        {
            addProjectReference(target, sourceProject);

            if (!target.References.ContainsReference(typeof(IContract).Assembly.Location))
                target.References.Add(typeof(IContract).Assembly.Location).CopyLocal = false;
        }

        /// <summary>
        /// Adds the project reference.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="reference">The reference.</param>
		private static void addProjectReference(VSProject2 project, Project reference)
        {
            if (project.References.Item(reference.Name) == null)
                project.References.AddProject(reference).CopyLocal = false;
        }

        /// <summary>
        /// Asserts the adapters not null.
        /// </summary>
        /// <param name="hostSideAdapterProject">The host side adapter project.</param>
        /// <param name="addInSideAdapterProject">The add in side adapter project.</param>
		private static void assertAdaptersNotNull(Project hostSideAdapterProject, Project addInSideAdapterProject)
        {
            if (hostSideAdapterProject == null)
                throw new ApplicationException("Host side adapter musn't be null.");

            if (addInSideAdapterProject == null)
                throw new ApplicationException("Add in side adapter musn't be null.");
        }

        /// <summary>
        /// Collapses the folder.
        /// </summary>
        /// <param name="hsa2">The hsa2.</param>
        /// <param name="asa2">The asa2.</param>
        /// <param name="folder">The folder.</param>
		private static void collapseFolder(VSProject2 hsa2, VSProject2 asa2, UIHierarchyItem folder)
        {
            foreach (UIHierarchyItem project in folder.UIHierarchyItems)
            {
                foreach (UIHierarchyItem projectItem in project.UIHierarchyItems)
                {
                    if (projectItem.Name == "References")
                        projectItem.UIHierarchyItems.Expanded = false;

                    else if (projectItem.Name.Equals(GENERETED_FILES_DIRECTORY) &&
                             (project.Name.Equals(hsa2.Project.Name) || project.Name.Equals(asa2.Project.Name)))
                        projectItem.UIHierarchyItems.Expanded = false;

                    collapseFolder(hsa2, asa2, projectItem);
                }

                if (project.Name == hsa2.Project.Name || project.Name == asa2.Project.Name)
                    project.UIHierarchyItems.Expanded = false;
            }
        }

        /// <summary>
        /// Builds the source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
		private static List<PipelineSegmentSource> buildSource(String source)
        {
            IPipelineBuilderWorker worker = new PipelineBuilderWorker();
            List<PipelineSegmentSource> sourceCode = worker.BuildPipeline(source);
            return sourceCode;
        }

        /// <summary>
        /// This function generated the project corresponding to the passed pipelineComponent.
        /// </summary>
        /// <param name="destPath">The destination path of the projects.</param>
        /// <param name="pipelineComponent">Which component to generate the project for.</param>
        /// <returns>The project generated.</returns>
        private Project createAddInProject(string destPath, PipelineSegmentSource pipelineComponent, PipelineConfiguration config)
        {
            // dest path will be the root of the project where we have addins,
            // so we need to select the correct project for this specific segment, if there is
            // one and take its path, otherwise do destPath.Combine(pipelineComponent.Name);
            string name;
            destPath = destPath.Combine(getSubPath(config, pipelineComponent, destPath, out name));
            var generatedDestPath = destPath.Combine(GENERETED_FILES_DIRECTORY);

            // the getSubPath will use the configuration to get the correct project,
            // if one was selected, and otherwise will return the name of the pipeline segment
            // so that getProjByName will return null if it's not existing and otherwise the correct
            // and selected project.
            Project proj = getProjByName(name);

            if (proj == null)
            {
                if (Directory.Exists(destPath))
                {
                    if (DialogResult.Yes == MessageBox.Show(string.Format("The directory {0} already exists, " +
                        " would you like to delete it and use it for the generated files for the segment named {1}?",
                        destPath, name), "Already existing directory", MessageBoxButtons.YesNoCancel))
                    {
                        Directory.Delete(destPath, true);
                    }
                    else return null;
                }

                Directory.CreateDirectory(destPath);

                string connectPath = Path.GetDirectoryName(typeof(PipelineBuilderCommand).Assembly.Location);

                proj = dte.Solution.AddFromTemplate(connectPath.Combine("Template.csproj"),
                                             destPath,
                                             pipelineComponent.Name + ".csproj",
                                             false);
            }

            if (proj == null) throw new InvalidOperationException("Problem in adding from template. "
                    + "You installation files for the pipeline builder may be corrupted.");

            if (Directory.Exists(generatedDestPath)) CheckSumValidator.ValidateCheckSum(generatedDestPath);

            deleteGeneratedFiles(proj);
            assertHasDirectory(proj, generatedDestPath);
            generateSourceFiles(pipelineComponent, generatedDestPath);
            addGeneratedFiles(proj, generatedDestPath);

            CheckSumValidator.StoreCheckSum(generatedDestPath);

            return proj;
        }

        /// <summary>
        /// Gets the sub path.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="component">The component.</param>
        /// <param name="root">The root.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
		private static string getSubPath(PipelineConfiguration configuration,
            PipelineSegmentSource component, string root,
            out string name)
        {
            string pathTakeRoot = name = component.Name;
            bool found;
            Project proj;

            switch (component.Type)
            {
                case SegmentType.AddInSideAdapter:
                    found = (proj = configuration.AddInSideAdapterProject) != null;
                    break;
                case SegmentType.AddInView:
                    found = (proj = configuration.AddInViewProject) != null;
                    break;
                case SegmentType.HostAddInView:
                    found = (proj = configuration.HostViewProject) != null;
                    break;
                case SegmentType.HostSideAdapter:
                    found = (proj = configuration.HostSideAdapterProject) != null;
                    break;
                case SegmentType.View:
                    found = (proj = configuration.AddInViewProject) != null;
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Doesn't know segment type: {0}", component.Type));
            }

            if (found)
            {
                assertNotOutsideRoot(proj, root);

                pathTakeRoot = proj.FullName.Substring(root.Length).Trim('\\', '/'); // remove root
                pathTakeRoot = pathTakeRoot.Substring(0, pathTakeRoot.Length - Path.GetFileName(pathTakeRoot).Length) // remove filename
                                    .Trim('\\', '/'); // firm it up a little
                name = proj.Name;
            }

            return pathTakeRoot;
        }

        /// <summary>
        /// Asserts the not outside root.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <param name="root">The root.</param>
		private static void assertNotOutsideRoot(Project project, string root)
        {
            if (!project.FullName.Contains(root))
                throw new InvalidOperationException(string.Format("The project {0} must be in directory {1}",
                                                                  project.Name, root));
        }

        /// <summary>
        /// Generates the source files.
        /// </summary>
        /// <param name="pipelineComponent">The pipeline component.</param>
        /// <param name="generatedDestPath">The generated dest path.</param>
		private static void generateSourceFiles(PipelineSegmentSource pipelineComponent, string generatedDestPath)
        {
            foreach (SourceFile source in pipelineComponent.Files)
            {
                using (var sw = new StreamWriter(generatedDestPath.Combine(source.Name)))
                {
                    sw.WriteLine(source.Source);
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Asserts the has directory.
        /// </summary>
        /// <param name="proj">The proj.</param>
        /// <param name="generatedDestPath">The generated dest path.</param>
		private static void assertHasDirectory(Project proj, string generatedDestPath)
        {
            if (File.Exists(generatedDestPath))
                throw new InvalidOperationException(string.Format("The path {0} must be to a directory and not a file.",
                                                                  generatedDestPath));

            var path = Path.GetFullPath(generatedDestPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // make sure we have a folder for the generated files.
            if (proj.ProjectItems.ContainsFolder(GENERETED_FILES_DIRECTORY)) return;
            proj.ProjectItems.AddFolder(GENERETED_FILES_DIRECTORY, null);
        }

        /// <summary>
        /// Sets the output path.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="type">The type.</param>
        /// <param name="root">The root.</param>
		private static void setOutputPath(Project p, SegmentType type, string root)
        {
            // Loop all configurations
            foreach (Configuration configuration in p.ConfigurationManager)
            {
                // Get the output path
                Property prop = configuration.Properties.Item("OutputPath");
                prop.Value = root.Replace(PathConstants.Configuration.Constant, configuration.ConfigurationName);

                switch (type)
                {
                    case SegmentType.AddInView:
                        prop.Value += "\\AddInViews";
                        break;
                    case SegmentType.AddInSideAdapter:
                        prop.Value += "\\AddInSideAdapters";
                        break;
                    case SegmentType.HostSideAdapter:
                        prop.Value += "\\HostSideAdapters";
                        break;
                    case SegmentType.HostAddInView:
                        break;
                    case SegmentType.View:
                        prop.Value += "\\AddInViews";
                        break;
                    default:
                        throw new InvalidOperationException("Not a valid pipeline component: " + p.Name);
                }
            }
        }

        #region Name & Project Helper Methods

        private Project getProjByName(string name)
        {
            foreach (Project p in GetProjectsFromSolution(dte))
                if (p.Name.Trim() == name)
                    return p;
            return null;
        }

        internal static List<Project> GetProjectsFromSolution(DTE2 root)
        {
            var projects = new List<Project>();

            foreach (Project proj in root.Solution.Projects)
            {
                if (proj.Kind != ProjectKinds.vsProjectKindSolutionFolder)
                    projects.Add(proj);
                else
                    projects.AddRange(getProjectsFromSolutionFolder(proj));
            }

            return projects;
        }

        private static List<Project> getProjectsFromSolutionFolder(Project slnFolder)
        {
            var projects = new List<Project>();

            foreach (ProjectItem projItem in slnFolder.ProjectItems)
            {
                Project proj = projItem.SubProject;

                if (proj == null) continue;

                if (!proj.Kind.Equals(ProjectKinds.vsProjectKindSolutionFolder))
                    projects.Add(proj);
                else
                    projects.AddRange(getProjectsFromSolutionFolder(proj));
            }

            return projects;
        }

        #endregion

        #region Custom file methods

        /// <summary>
        /// Deletes the generated files in a specific project and nothing more.
        /// </summary>
        /// <param name="project">Project to remove the generated files from</param>
        private static void deleteGeneratedFiles(Project project)
        {
            ProjectItem projectItem;
            try
            {
                projectItem = project.ProjectItems.Item(GENERETED_FILES_DIRECTORY);
            }
            catch (ArgumentException)
            {
                // There are no generated files in this project,
                // so ProjectItems.Item(Object index) throws argument exception.
                return;
            }

            deleteFilesFromProjectItem(projectItem,
                                       /* the pipeline builder generates no folders, so don't delete them: */
                                       x => false,
                                       /* files to delete: */
                                       s => s.Contains(".generated.cs") || s.Contains(".g.cs"));
        }

        /// <summary>
        /// Adds the generated files to a specific project but does not include the subversion files.
        /// </summary>
        /// <param name="project">Project to add the generated files to</param>
        /// <param name="directory">Directory that contains the generated files</param>
        private static void addGeneratedFiles(Project project, string directory)
        {
            addFilesToProject(project, directory,
                /* don't include .svn files */        d => d == ".svn",
                /* and don't include "hash" dirs */ f => f.ToLower() == "hash");
        }

        /// <summary>
        /// Deletes all files in a specific project item
        /// </summary>
        /// <param name="parent">Parent project item</param>
        /// <param name="delDir">Function, returning true if we are to delete this directory.</param>
        /// <param name="delFile">Function, returning true if we are to delete this file.</param>
        private static void deleteFilesFromProjectItem(ProjectItem parent, Func<string, bool> delDir,
                                                       Func<string, bool> delFile)
        {
            // Loop all child items
            foreach (ProjectItem projectItem in parent.ProjectItems)
            {
                // Loop all the files (but start at 1 because 0 is the "parent")
                for (short i = 0; i < projectItem.FileCount; i++)
                {
                    // Get filename
                    string fileName = projectItem.get_FileNames(i);

                    if (Directory.Exists(fileName))
                    {
                        // Get directory info
                        var directoryInfo = new DirectoryInfo(fileName);
                        if (delDir(directoryInfo.Name))
                        {
                            // Get project item and then del it
                            ProjectItem directoryProjectItem = parent.ProjectItems.Item(directoryInfo.Name);
                            deleteFilesFromProjectItem(directoryProjectItem, delDir, delFile);
                        }
                    }
                    else if (File.Exists(fileName)) // Check if this is a file
                    {
                        var fileInfo = new FileInfo(fileName);
                        if (delFile(fileInfo.Name))
                        {
                            File.Delete(fileName);
                            projectItem.Remove();

                            // Removing one, moves items below, further up, so dec index.
                            i--;
                        }
                    }
                    else projectItem.Remove(); // This is not an existing directory or file, just remove it from the project	
                }
            }
        }

        /// <summary>
        /// Adds all files in a specific directory to a specific project
        /// </summary>
        /// <param name="project">The project we're looking at.</param>
        /// <param name="directory">Directory on disk to include</param>
        /// <param name="isDirException">Function, returns true if the directory is an exception and shouldn't be added.</param>
        /// <param name="isFileException">Function, returns true if the file is an exception and shouldn't be added.</param>
        private static void addFilesToProject(Project project, string directory,
                                              Func<string, bool> isDirException,
                                              Func<string, bool> isFileException)
        {
            var topDirectoryInfo = new DirectoryInfo(directory);

            // Add all files
            foreach (FileInfo fileInfo in topDirectoryInfo.GetFiles())
            {
                if (isFileException(fileInfo.Name)) continue;
                project.ProjectItems.AddFromFile(fileInfo.FullName);
            }

            foreach (DirectoryInfo directoryInfo in topDirectoryInfo.GetDirectories())
            {
                if (isDirException(directoryInfo.Name)) continue;
                addFilesToProject(project, directoryInfo.FullName, isDirException, isFileException);
            }
        }

        #endregion

        #region Error handling

        private static void showException(Exception e)
        {
            Console.WriteLine(e);
            var error = new DisplayError();
            error.SetError(e);
            error.Show();
        }

        #endregion
    }
}
