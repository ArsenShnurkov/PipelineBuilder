using System;
using System.IO;
using EnvDTE;
using VSLangProj;
using VSLangProj2;

namespace VSPipelineBuilder
{
	public static class ProjectExtensions
	{
        /// <summary>
        /// Combines the specified paths.
        /// </summary>
        /// <param name="path1">The first path.</param>
        /// <param name="path2">The second path.</param>
        /// <returns>Combined path of path1 and path2.</returns>
		public static string Combine(this string path1, string path2)
		{
			if (path2 == null) throw new ArgumentNullException("path2");

			return Path.Combine(path1, path2);
		}


		public static bool ContainsFolder(this ProjectItems items, string folderName)
		{
			if (folderName == null) throw new ArgumentNullException("folderName");

			foreach (var item in items)
			{
				if (((ProjectItem)item).Name == folderName)
					return true;
			}
			return false;
		}

		public static bool ContainsReference(this References refs, string assemblyLocation)
		{
			foreach (var r in refs)
			{
				var reference = (Reference2)r;
				if (reference.Path == assemblyLocation)
					return true;
			}
			return false;
		}
	}
}