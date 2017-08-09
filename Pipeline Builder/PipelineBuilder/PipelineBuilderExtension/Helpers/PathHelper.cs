using System.IO;
using System.Windows.Forms;

namespace PipelineBuilderExtension
{
    /// <summary>
    /// Path helper class.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Lets the user select a directory
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>
        /// Selected path by the user or <c>null</c> if no path was selected.
        /// </returns>
        public static string SelectDirectory(string description)
        {
            // Invoke override
            return SelectDirectory(description, null);
        }

        /// <summary>
        /// Lets the user select a directory
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="initialDirectory">The initial directory.</param>
        /// <returns>
        /// Selected path by the user or <c>null</c> if no path was selected.
        /// </returns>
        public static string SelectDirectory(string description, string initialDirectory)
        {
            // Create dialog
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog() { ShowNewFolderButton = true };
            folderBrowserDialog.Description = description;
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                folderBrowserDialog.SelectedPath = initialDirectory;
            }

            // Show dialog
            return (folderBrowserDialog.ShowDialog() == DialogResult.OK) ? folderBrowserDialog.SelectedPath : null;
        }
    }
}
