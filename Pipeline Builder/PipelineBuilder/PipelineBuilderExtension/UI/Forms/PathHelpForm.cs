using System.Windows.Forms;
using PipelineBuilder.Data;

namespace PipelineBuilderExtension.UI.Forms
{
    public partial class PathHelpForm : Form
    {
        #region Variables
        #endregion

        #region Constructor & destructor
        /// <summary>
        /// Initializes a new instance of the <see cref="PathHelpForm"/> class.
        /// </summary>
        public PathHelpForm()
        {
            // Initialize component
            InitializeComponent();

            // Initialize paths
            InitializePaths();
        }
        #endregion;

        #region Properties
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the paths.
        /// </summary>
        private void InitializePaths()
        {
            // Clear
            constantsListView.Items.Clear();

            // Get all path constants
            PathConstant[] pathConstants = PathConstants.GetAllPathConstants();
            foreach (PathConstant pathConstant in pathConstants)
            {
                // Create listview item
                ListViewItem listViewItem = new ListViewItem(pathConstant.Constant);
                listViewItem.SubItems.Add(pathConstant.Description);

                // Add listview item
                constantsListView.Items.Add(listViewItem);
            }
        }
        #endregion
    }
}