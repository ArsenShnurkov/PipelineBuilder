using System;
using System.AddIn.Contract;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;
using PipelineBuilder;
using VSLangProj2;
using VSLangProj80;
using StatusBar=EnvDTE.StatusBar;

namespace VSPipelineBuilder
{
	/// <summary>
	/// Connect class for the pipeline manager.
	/// </summary>
	[Serializable]
	public class Connect : IDTExtensibility2, IDTCommandTarget
	{
		private const string PIPELINE_BUILDER_NAME = "VSPipelineBuilder.Connect.PipelineBuilder";
		private const string GENERETED_FILES_DIRECTORY = "Generated Files";
		private AddIn _AddInInstance;
		private DTE2 _ApplicationObject;
		private bool _UIInitialized;
		private PipelineConfiguration config;

		public Connect()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			config = new PipelineConfiguration();
		}

		#region IDTCommandTarget Members

		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, 
								vsCommandStatusTextWanted neededText, 
								ref vsCommandStatus status,
		                        ref object commandText)
		{
			if (neededText != vsCommandStatusTextWanted.vsCommandStatusTextWantedNone) return;
			if (commandName != PIPELINE_BUILDER_NAME) return;

			status = vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
		}

		/// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
		/// <param term='commandName'>The name of the command to execute.</param>
		/// <param term='executeOption'>Describes how the command should be run.</param>
		/// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
		/// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
		/// <param term='handled'>Informs the caller if the command was handled or not.</param>
		/// <seealso class='Exec' />
		public void Exec(string commandName, vsCommandExecOption executeOption, 
						 ref object varIn, 
						 ref object varOut,
		                 ref bool handled)
		{
			handled = false;

			if (executeOption != vsCommandExecOption.vsCommandExecOptionDoDefault
			    || commandName != PIPELINE_BUILDER_NAME)
				return;

			handled = true;

			LoadMe();

			StatusBar statusBar = null;
			try
			{
				config.Initialize(_ApplicationObject);

				if (config.ShowDialog() != DialogResult.OK) return;

				statusBar = config.SourceProject.DTE.StatusBar;
				buildPipeline(config, (curr, total) => /* update lambda */ 
				{ 
					if (curr != total) statusBar.Progress(true, "Generating Pipeline", curr, total);
					else statusBar.Progress(false, "Pipeline Generation Complete", 1, 1);
				});
			}
			catch (Exception e)
			{
				showException(e);

				if (statusBar != null)
				{
					statusBar.Clear();
					statusBar.Progress(false, "Problem generating pipeline.", 1, 1);
				}
				
			}
		}

		#endregion

		#region IDTExtensibility2 Members

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
			_ApplicationObject = (DTE2) application;
			_AddInInstance = (AddIn) addInInst;

			if (_UIInitialized ||
			    (connectMode != ext_ConnectMode.ext_cm_UISetup && connectMode != ext_ConnectMode.ext_cm_Startup))
				return;

			_UIInitialized = true;

			//Place the command on the tools menu.
			//Find the MenuBar command bar, which is the top-level command bar holding all the main menu items:
			CommandBar menuBarCommandBar = ((CommandBars) _ApplicationObject.CommandBars)["MenuBar"];

			//Find the Tools command bar on the MenuBar command bar:
			var toolsPopup = (CommandBarPopup) menuBarCommandBar.Controls[getToolsMenuName()];
			var commands = (Commands2) _ApplicationObject.Commands;
			var contextGUIDs = new object[] {};
			//This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
			//  just make sure you also update the QueryStatus/Exec method to include the new command names.
			try
			{
				//Add a command to the Commands collection:
				Command command = commands.AddNamedCommand2(_AddInInstance, "PipelineBuilder", "PipelineBuilder",
				                                            "Executes the command for PipelineBuilder", true, 59, ref contextGUIDs,
				                                            (int) vsCommandStatus.vsCommandStatusSupported +
				                                            (int) vsCommandStatus.vsCommandStatusEnabled,
				                                            (int) vsCommandStyle.vsCommandStylePictAndText,
				                                            vsCommandControlType.vsCommandControlTypeButton);

				//Add a control for the command to the tools menu:
				if ((command != null) && (toolsPopup != null))
					command.AddControl(toolsPopup.CommandBar, 1);
			}
			catch (ArgumentException)
			{
				// If we are here, then the exception is probably because a command with that name
				// already exists. If so there is no need to recreate the command and we can 
				// safely ignore the exception.
			}
		}

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}

		#endregion

		#region Setting up VS interface

		private string getToolsMenuName()
		{
			string toolsMenuName = "Tools";

			// If you would like to move the command to a different menu, change the word "Tools" to the 
			// English version of the menu. This code will take the culture, append on the name of the menu
			// then add the command to that menu. You can find a list of all the top-level menus in the file
			// CommandBar.resx.
			var resourceManager = new ResourceManager("VSPipelineBuilder.CommandBar",
			                                          Assembly.GetExecutingAssembly());

			var cultureInfo = new CultureInfo(_ApplicationObject.LocaleID);

			string resourceName = cultureInfo.TwoLetterISOLanguageName + "Tools";

			try
			{
				toolsMenuName = resourceManager.GetString(resourceName);
			}
			catch (MissingManifestResourceException) // unable to find language
			{
			}

			return toolsMenuName;
		}

		private void LoadMe()
		{
			LoadFromPromotion();
		}

		private static void LoadFromPromotion()
		{
			Assembly connectAssembly = typeof (Connect).Assembly;

			if (connectAssembly == null)
			{
				showException(new ApplicationException("Unable to find the connect assembly."));
				return;
			}

			Assembly.Load(connectAssembly.FullName);

			Assembly piplineBuilderAsm = typeof (PipelineBuilder.PipelineBuilder).Assembly;

			if (piplineBuilderAsm == null)
			{
				showException(new ApplicationException("Unable to find the pipelineBuilder."));
				return;
			}

			Assembly.Load(piplineBuilderAsm.FullName);
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = new AssemblyName(args.Name);

			if (args.Name == typeof (Connect).Assembly.FullName)
				return typeof (Connect).Assembly;

			string myPath = Path.GetDirectoryName(typeof (Connect).Assembly.Location);

			if (name.Name == "PipelineBuilder")
				return Assembly.LoadFrom(myPath.Combine("PipelineBuilder.dll"));

			return name.Name == "PipelineHints" ? Assembly.LoadFrom(myPath.Combine("PipelineBuilder.dll")) : null;
		}

		#endregion

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
				Project p;

				// if this returns null, then we have cancelled from the createAddInProject.
				// also create new add in project.
				if ((p = createAddInProject(destPath, pipelineSegmentSource, config)) == null)
					return;

				setOutputPath(p, pipelineSegmentSource.Type, outputPath);

				switch (pipelineSegmentSource.Type)
				{
					case SegmentType.HostAddInView:
						hostAddInView = p;
						break;
					case SegmentType.HostSideAdapter:
						hostSideAdapterProject = p;
						break;
					case SegmentType.AddInSideAdapter:
						addInSideAdapterProject = p;
						break;
					case SegmentType.AddInView:
						addInView = p;
						break;
					case SegmentType.View:
						view = p;
						break;
				}

				reportProgress(progress++, sources.Count);
			}

			assertAdaptersNotNull(hostSideAdapterProject, addInSideAdapterProject);

			var hostSideAdapter2 = (VSProject2) hostSideAdapterProject.Object;
			var addInSideAdapter2 = (VSProject2) addInSideAdapterProject.Object;

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

			reportProgress(1,1);
			var dte2 = (DTE2) sourceProject.DTE;

			foreach (UIHierarchyItem solution in dte2.ToolWindows.SolutionExplorer.UIHierarchyItems)
				collapseFolder(hostSideAdapter2, addInSideAdapter2, solution);
		}

		private static void addContractReference(VSProject2 target, Project sourceProject)
		{
			addProjectReference(target, sourceProject);

			if (!target.References.ContainsReference(typeof (IContract).Assembly.Location))
				target.References.Add(typeof (IContract).Assembly.Location).CopyLocal = false;
		}

		private static void addProjectReference(VSProject2 project, Project reference)
		{
			if (project.References.Item(reference.Name) == null)
				project.References.AddProject(reference).CopyLocal = false;
		}

		private static void assertAdaptersNotNull(Project hostSideAdapterProject, Project addInSideAdapterProject)
		{
			if (hostSideAdapterProject == null)
				throw new ApplicationException("Host side adapter musn't be null.");

			if (addInSideAdapterProject == null)
				throw new ApplicationException("Add in side adapter musn't be null.");
		}

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

				string connectPath = Path.GetDirectoryName(typeof(Connect).Assembly.Location);

				proj = _ApplicationObject.Solution.AddFromTemplate(connectPath.Combine("Template.csproj"),
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

		private static void assertNotOutsideRoot(Project project, string root)
		{
			if (!project.FullName.Contains(root))
				throw new InvalidOperationException(string.Format("The project {0} must be in directory {1}",
				                                                  project.Name, root));
		}

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

		private static void setOutputPath(Project p, SegmentType type, string root)
		{
			foreach (Configuration c in p.ConfigurationManager)
			{
				Property prop = c.Properties.Item("OutputPath");
				prop.Value = root;

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
			foreach (Project p in GetProjectsFromSolution(_ApplicationObject))
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
			catch (ArgumentException) { 
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
				/* don't include .svn files */		d => d == ".svn", 
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
			foreach (FileInfo fileInfo in  topDirectoryInfo.GetFiles())
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