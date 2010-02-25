namespace PipelineBuilder.Data
{
    /// <summary>
    /// Path constant data class.
    /// </summary>
    public class PathConstant
    {
        #region Variables
        #endregion

        #region Constructor & destructor
        /// <summary>
        /// Initializes a new instance of the <see cref="PathConstant"/> class.
        /// </summary>
        /// <param name="constant">The constant.</param>
        /// <param name="description">The description.</param>
        public PathConstant(string constant, string description)
        {
            // Store values
            Constant = constant;
            Description = description;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the constant.
        /// </summary>
        /// <value>The constant.</value>
        public string Constant { get; private set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; private set; }
        #endregion

        #region Methods
        #endregion
    }
}
