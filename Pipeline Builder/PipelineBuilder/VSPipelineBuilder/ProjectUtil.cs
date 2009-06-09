using System;
using System.IO;
using EnvDTE;

namespace VSPipelineBuilder
{
	internal static class ProjectUtil
	{
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