using System;
using System.Linq;
using System.Reflection;

namespace PipelineBuilder.Data
{
    /// <summary>
    /// Class containing path constants
    /// </summary>
    public static class PathConstants
    {
        /// <summary>
        /// Configuration constant.
        /// </summary>
        public static readonly PathConstant Configuration = new PathConstant("%configuration%", "Configuration name, such as 'Debug' or 'Release'.");

        /// <summary>
        /// Gets all path constants.
        /// </summary>
        /// <returns>Array of <see cref="PathConstant"/> objects.</returns>
        public static PathConstant[] GetAllPathConstants()
        {
            // Use reflection
            Type pathConstantsType = typeof(PathConstants);

            // Get all properties
            FieldInfo[] fields = pathConstantsType.GetFields();
            var result = (from field in fields
                         where field.FieldType == typeof(PathConstant)
                         select (PathConstant)field.GetValue(null)).ToArray();

            // Return result
            return result;
        }
    }
}
