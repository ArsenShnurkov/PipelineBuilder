using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace PipelineBuilderExtension.UI.Controls
{
    /// <summary>
    /// Step number.
    /// </summary>
    public partial class StepNumber : UserControl
    {
        #region Enumerations
        /// <summary>
        /// Box style type.
        /// </summary>
        public enum BoxStyleType
        {
            /// <summary>
            /// Ellipse.
            /// </summary>
            Ellipse,

            /// <summary>
            /// Rectangle.
            /// </summary>
            Rectangle,

            /// <summary>
            /// Rounded rectangle.
            /// </summary>
            RoundedRectangle
        }

        /// <summary>
        /// Text alignment style.
        /// </summary>
        public enum TextAlignStyleType
        {
            /// <summary>
            /// Top left.
            /// </summary>
            TopLeft,

            /// <summary>
            /// Top center.
            /// </summary>
            TopCenter,

            /// <summary>
            /// Top right.
            /// </summary>
            TopRight,

            /// <summary>
            /// Middle left.
            /// </summary>
            MiddleLeft,

            /// <summary>
            /// Middle center.
            /// </summary>
            MiddleCenter,

            /// <summary>
            /// Middle right.
            /// </summary>
            MiddleRight,

            /// <summary>
            /// Bottom left.
            /// </summary>
            BottomLeft,

            /// <summary>
            /// Bottom center.
            /// </summary>
            BottomCenter,

            /// <summary>
            /// Bottom right.
            /// </summary>
            BottomRight
        }
        #endregion

        #region Variables
        private Color _borderColor;
        private float _borderSize;
        private Color _brushColor;
        private Color _textColor;

        private string _text;
        private string _textSplitter;
        private string _helpText;

        private int _textOffset;

        private BoxStyleType _boxStyle;
        private TextAlignStyleType _textAlignStyle;
        #endregion

        #region Constructor & destructor
        /// <summary>
        /// Default constructor for this control
        /// </summary>
        public StepNumber()
        {
            // Initialize component
            InitializeComponent();

            // Set default values
            _borderColor = Color.FromArgb(125, 125, 125);
            _borderSize = 2.0f;
            _brushColor = Color.FromArgb(244, 199, 0);
            _textColor = Color.White;
            
            _text = "";
            _textSplitter = ")";
            _helpText = "";
            AutoSize = false;
            Font = new Font(Font, FontStyle.Bold);
            Size = new Size(30, 30);

            _textOffset = 10;

            _boxStyle = BoxStyleType.RoundedRectangle;
            _textAlignStyle = TextAlignStyleType.MiddleCenter;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the border color
        /// </summary>
        public Color BorderColor
        {
            get
            {
                return _borderColor;
            }
            set
            {
                _borderColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the border size
        /// </summary>
        [Description("Border size"), DefaultValue(2.0f)]
        public float BorderSize
        {
            get
            {
                return _borderSize;
            }
            set
            {
                _borderSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the brush color
        /// </summary>
        [Description("Color of the brush (solid color)")]
        public Color BrushColor
        {
            get
            {
                return _brushColor;
            }
            set
            {
                _brushColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the text color
        /// </summary>
        [Description("Color of the text")]
        public Color TextColor
        {
            get
            {
                return _textColor;
            }
            set
            {
                _textColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the text to display
        /// </summary>
        [Description("Step text to display, if empty the number will be automatically determined"), DefaultValue("")]
        public override string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }

        /// <summary>
        /// Gets or sets the text splitter to split the text and help text
        /// </summary>
        [Description("Splitter text that will split the text and help text"), DefaultValue(")")]
        public string TextSplitter
        {
            get
            {
                return _textSplitter;
            }
            set
            {
                _textSplitter = value;
            }
        }

        /// <summary>
        /// Gets or sets the help text to display
        /// </summary>
        [Description("Help text with additional step information"), DefaultValue("")]
        public string HelpText
        {
            get
            {
                return _helpText;
            }
            set
            {
                _helpText = value;
            }
        }

        /// <summary>
        /// Gets or sets the text offset when the BoxStyle is not Ellipse
        /// </summary>
        [Description("Offset of the text when the BoxStyle is not Ellipse"), DefaultValue(10)]
        public int TextOffset
        {
            get
            {
                return _textOffset;
            }
            set
            {
                _textOffset = value;
            }
        }

        /// <summary>
        /// Gets or sets the style to use for drawing the box
        /// </summary>
        [Description("Style of the box"), DefaultValue(StepNumber.BoxStyleType.RoundedRectangle)]
        public BoxStyleType BoxStyle
        {
            get
            {
                return _boxStyle;
            }
            set
            {
                _boxStyle = value;
            }
        }

        /// <summary>
        /// Gets or sets the text alignement style
        /// </summary>
        [Description("Text alignement style"), DefaultValue(StepNumber.TextAlignStyleType.MiddleCenter)]
        public TextAlignStyleType TextAlignStyle
        {
            get
            {
                return _textAlignStyle;
            }
            set
            {
                _textAlignStyle = value;
            }
        }
        #endregion

        #region Methods
        private void StepNumber_Resize(object sender, EventArgs e)
        {
            // Invalidate
            Invalidate();
        }

        /// <summary>
        /// Automatically calculates the tab number so the user doesn't have to manually set it
        /// </summary>
        /// <returns>Tab number to show</returns>
        private int CalculateTabNumber()
        {
            // Declare variables
            int tabNumber = 1;

            // Check the number of items in the control
            foreach (Control control in Parent.Controls)
            {
                // Is this control of type StepNumber?
                if (control.GetType() == this.GetType())
                {
                    // Is this the current control?
                    if (control != this)
                    {
                        // Is the tabindex of the control lower than this control?
                        if (control.TabIndex < this.TabIndex)
                        {
                            // Yes, increase tabnumber
                            tabNumber++;
                        }
                    }
                }
            }

            // Return tab number
            return tabNumber;
        }

        /// <summary>
        /// Paints the control
        /// </summary>
        /// <param name="e">PaintEventArgs</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            // Call base function
            base.OnPaint(e);

            // Define offsets and radius
            int borderOffset = 1;
            int cornerRadius = 3;

            // Get graphics
            Graphics g = e.Graphics;

            // Get size
            SizeF ellipseSize = (SizeF)ClientSize;

            // Calculate font size and text position
            string text = (_text != "") ? _text : CalculateTabNumber().ToString();
            text += (_helpText != "") ? string.Format("{0} {1}", _textSplitter, _helpText) : "";
            SizeF textSize = g.MeasureString(text, Font);

            // Should we autosize?
            if (AutoSize)
            {
                // Yes, autosize & draw rounded rectangle
                ClientSize = new Size((int)(textSize.Width + (4 * _textOffset)), (int)(textSize.Height + (2 * _textOffset)));

                // Get size (again)
                ellipseSize = (SizeF)ClientSize;
            }

            #region Calculate text position
            float x = _textOffset;
            float y = _textOffset; 
            switch (_textAlignStyle)
            {
                case TextAlignStyleType.TopLeft:
                    x = _textOffset;
                    y = _textOffset;
                    break;

                case TextAlignStyleType.TopCenter:
                    x = (ellipseSize.Width / 2) - (textSize.Width / 2);
                    y = _textOffset;
                    break;

                case TextAlignStyleType.TopRight:
                    x = ellipseSize.Width - textSize.Width - _textOffset;
                    y = _textOffset;
                    break;

                case TextAlignStyleType.MiddleLeft:
                    x = _textOffset;
                    y = (ellipseSize.Height / 2) - (textSize.Height / 2);
                    break;

                case TextAlignStyleType.MiddleCenter:
                    x = (ellipseSize.Width / 2) - (textSize.Width / 2);
                    y = (ellipseSize.Height / 2) - (textSize.Height / 2);
                    break;

                case TextAlignStyleType.MiddleRight:
                    x = ellipseSize.Width - textSize.Width - _textOffset;
                    y = (ellipseSize.Height / 2) - (textSize.Height / 2);
                    break;

                case TextAlignStyleType.BottomLeft:
                    x = _textOffset;
                    y = ellipseSize.Height - textSize.Height - _textOffset;
                    break;

                case TextAlignStyleType.BottomCenter:
                    x = (ellipseSize.Width / 2) - (textSize.Width / 2);
                    y = ellipseSize.Height - textSize.Height - _textOffset;
                    break;

                case TextAlignStyleType.BottomRight:
                    x = ellipseSize.Width - textSize.Width - _textOffset;
                    y = ellipseSize.Height - textSize.Height - _textOffset;
                    break;
            }
            #endregion

            // Enable anti aliasing
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            #region Determine box style
            switch (_boxStyle)
            {
                case BoxStyleType.Ellipse:
                    // Draw ellipse
                    g.FillEllipse(new SolidBrush(_brushColor), new Rectangle(borderOffset, borderOffset, (int)(ellipseSize.Width - _borderSize - borderOffset), (int)(ellipseSize.Height - _borderSize - borderOffset)));
                    g.DrawEllipse(new Pen(_borderColor, _borderSize), new Rectangle(borderOffset, borderOffset, (int)(ellipseSize.Width - _borderSize - borderOffset), (int)(ellipseSize.Height - _borderSize - borderOffset)));
                    break;

                case BoxStyleType.Rectangle:
                    // Draw rectangle
                    g.FillRectangle(new SolidBrush(_brushColor), new Rectangle(borderOffset, borderOffset, (int)(ellipseSize.Width - _borderSize - borderOffset), (int)(ellipseSize.Height - _borderSize - borderOffset)));
                    g.DrawRectangle(new Pen(_borderColor, _borderSize), new Rectangle(borderOffset, borderOffset, (int)(ellipseSize.Width - _borderSize - borderOffset), (int)(ellipseSize.Height - _borderSize - borderOffset)));
                    break;

                case BoxStyleType.RoundedRectangle:
                    // Create path
                    GraphicsPath path = CreateRoundedCornerRectangle(borderOffset, borderOffset, ellipseSize.Width - _borderSize - borderOffset,
                        ellipseSize.Height - _borderSize - borderOffset, cornerRadius);

                    // Draw rounded rectangle
                    g.FillPath(new SolidBrush(_brushColor), path);
                    g.DrawPath(new Pen(_borderColor, _borderSize), path);

                    // Dispose path
                    path.Dispose();
                    break;
            }
            #endregion

            // Draw string
            g.DrawString(text, Font, new SolidBrush(_textColor), x, y);
        }

        /// <summary>
        /// Creates a graphics path that represents a rounded rectangle
        /// </summary>
        /// <param name="x">Location x of the rectangle</param>
        /// <param name="y">Location y of the rectangle</param>
        /// <param name="width">Width of the rectangle</param>
        /// <param name="height">Height of the rectangle</param>
        /// <param name="cornerRadius">Radius of the rounded corners</param>
        /// <returns>GraphicsPath object that represents a rounded rectangle</returns>
        /// <remarks>Don't forget to clean up the returned GraphicsPath after usage</remarks>
        public GraphicsPath CreateRoundedCornerRectangle(float x, float y, float width, float height, float cornerRadius)
        {
            // Create graphics path
            GraphicsPath gfxPath = new GraphicsPath();

            // Set up path
            gfxPath.AddLine(x + cornerRadius, y, x + width - (cornerRadius * 2), y);
            gfxPath.AddArc(x + width - (cornerRadius * 2), y, cornerRadius * 2, cornerRadius * 2, 270, 90);
            gfxPath.AddLine(x + width, y + cornerRadius, x + width, y + height - (cornerRadius * 2));
            gfxPath.AddArc(x + width - (cornerRadius * 2), y + height - (cornerRadius * 2), cornerRadius * 2, cornerRadius * 2, 0, 90);
            gfxPath.AddLine(x + width - (cornerRadius * 2), y + height, x + cornerRadius, y + height);
            gfxPath.AddArc(x, y + height - (cornerRadius * 2), cornerRadius * 2, cornerRadius * 2, 90, 90);
            gfxPath.AddLine(x, y + height - (cornerRadius * 2), x, y + cornerRadius);
            gfxPath.AddArc(x, y, cornerRadius * 2, cornerRadius * 2, 180, 90);
            gfxPath.CloseFigure();

            // Return result
            return gfxPath;
        }
        #endregion
    }
}
