using System;
using System.IO;
using EnvDTE;

namespace PipelineBuilderExtension
{
	internal static class ProjectHelper
	{
        /// <summary>
        /// Gets the output assembly of a specific project.
        /// </summary>
        /// <param name="p">The project.</param>
        /// <returns>Output assembly.</returns>
		internal static string GetOutputAssembly(this Project p)
		{
			if (p == null) throw new ArgumentNullException("p");
			var properties = p.ConfigurationManager.ActiveConfiguration.Properties;
			var outputPath = Path.GetDirectoryName(properties.Item("OutputPath").Value.ToString());
			var outputFileName = string.Empty;

			try
			{
				outputFileName = properties.Item("OutputFileName").Value.ToString();
			}
			catch (ArgumentException)
			{
				outputFileName = Path.GetFileNameWithoutExtension(p.FileName) + ".dll";
			}

			return Path.GetDirectoryName(p.FullName).Combine(outputPath).Combine(outputFileName);
		}
	}
}