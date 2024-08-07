using FB.VisualFB;
using NtoLib.NumberTextBox;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.TextBoxInt
{
    [ComVisible(true)]
    [Guid("02D828FA-39A4-4363-A58A-C2DEE974C04F")]
    [DisplayName("Целочисленное поле")]
    public partial class TextBoxIntControl : VisualControlBase
    {
        [Category("Внешний вид")]
        [DisplayName("Цвет рамки")]
        public override Color BackColor { get; set; }

        private string _textBefore;
        [Category("Внешний вид")]
        [DisplayName("Текст до")]
        public string TextBefore
        {
            get
            {
                return _textBefore;
            }
            set
            {
                _textBefore = value;
                UpdateText();
            }
        }

        private string _textAfter;
        [Category("Внешний вид")]
        [DisplayName("Текст после")]
        public string TextAfter
        {
            get
            {
                return _textAfter;
            }
            set
            {
                _textAfter = value;
                UpdateText();
            }
        }

        [Category("Внешний вид")]
        [DisplayName("Выравнивание")]
        public HorizontalAlignment Alignment
        {
            get => textBox.TextAlign;
            set => textBox.TextAlign = value;
        }

        private int _maxValue;
        [Category("Значение")]
        [DisplayName("Максимальное значение")]
        public int MaxValue
        {
            get
            {
                return _maxValue;
            }
            set
            {
                _maxValue = value;
                if(_maxValue < _minValue)
                    _maxValue = _minValue;
            }
        }

        private int _minValue;
        [Category("Значение")]
        [DisplayName("Минимальное значение")]
        public int MinValue
        {
            get
            {
                return _minValue;
            }
            set
            {
                _minValue = value;
                if(_minValue > _maxValue)
                    _minValue = MaxValue;
            }
        }

        [Category("Я - запретная зона")]
        [DisplayName("Шрифт")]
        [Browsable(false)]
        public Font TextBoxFont
        {
            get
            { 
                return textBox.Font; 
            }
            set
            {
                textBox.Font = value;
            }
        }

        private int _value;
        private bool _isInitialized = false;



        public TextBoxIntControl()
        {
            InitializeComponent();

            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }



        protected override void ToRuntime()
        {
            base.ToRuntime();

            _isInitialized = true;

            ValidateValue();
            textBox.ValidatingValue += ValidateValue;
        }

        protected override void ToDesign()
        {
            base.ToDesign();

            _isInitialized = false;

            ValidateValue();
            textBox.ValidatingValue -= ValidateValue;
        }

        private void HandleResize(object sender, EventArgs e)
        {
            UpdateTextBoxFontSize();
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            textBox.Location = new Point(1, 1);
            textBox.Width = Width - 2;
        }

        private void UpdateTextBoxFontSize()
        {
            float size = (Height - 2f - 1f) / 1.525f;
            size = size <= 0 ? 1 : size;

            Font font = new Font(textBox.Font.FontFamily, size, FontStyle.Regular);
            textBox.Font = font;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if(!DesignMode)
            {
                int value = FBConnector.GetPinInt(TextBoxIntFB.OutputToControlId);
                textBox.Text = value.ToString();

                if(_isInitialized)
                    FBConnector.SetPinValue(TextBoxIntFB.InputFromControlId, value);
            }
        }



        private void UpdateText()
        {
            string text = _value.ToString();

            if(!string.IsNullOrEmpty(TextBefore))
                text = TextBefore + ' ' + text;

            if(!string.IsNullOrEmpty(TextAfter))
                text = text + ' ' + TextAfter;

            textBox.Text = text;
        }

        private void ValidateValue()
        {
            ReadResult readResult = ReadValue(out var value);

            if(readResult != ReadResult.Success)
            {
                string message;
                switch(readResult)
                {
                    case ReadResult.AboveMax:
                    {
                        message = $"Значение должно быть меньше {MaxValue}";
                        break;
                    }
                    case ReadResult.BelowMin:
                    {
                        message = $"Значение должно быть больше {MinValue}";
                        break;
                    }
                    case ReadResult.ParseError:
                    {
                        message = "Ошибка ввода";
                        break;
                    }
                    default:
                    {
                        throw new NotImplementedException();
                    }
                }

                MessageBox.Show(message);
            }
            else
            {
                _value = value;

                if(!DesignMode)
                    FBConnector.SetPinValue(TextBoxIntFB.InputFromControlId, _value);
            }

            UpdateText();

            textBox.Enabled = false;
            textBox.Enabled = true;
        }

        private ReadResult ReadValue(out int value)
        {
            value = 0;

            if(string.IsNullOrEmpty(textBox.Text))
            {
                value = 0;
                return ReadResult.Success;
            }
            else if(!int.TryParse(textBox.Text, out var number))
            {
                return ReadResult.ParseError;
            }
            else if(number < MinValue)
            {
                return ReadResult.BelowMin;
            }
            else if(number > MaxValue)
            {
                return ReadResult.AboveMax;
            }
            else
            {
                value = number;
                return ReadResult.Success;
            }
        }
    }
}
