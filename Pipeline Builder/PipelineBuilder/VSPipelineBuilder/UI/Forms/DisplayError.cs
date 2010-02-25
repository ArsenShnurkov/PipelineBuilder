using System.Windows.Forms;

namespace VSPipelineBuilder.UI.Forms
{
    public partial class DisplayError : Form
    {
        public DisplayError()
        {
            InitializeComponent();
        }

        public void SetError(object error)
        {
            t_errorText.Text = error.ToString();
        }
    }
}