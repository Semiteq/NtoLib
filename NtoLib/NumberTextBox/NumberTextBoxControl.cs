using FB.VisualFB;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.NumberTextBox
{
    [ComVisible(true)]
    [Guid("73456D0F-FEE5-4DA7-9089-0E284AFF00A0")]
    [DisplayName("Числовое поле")]
    public partial class NumberTextBoxControl : VisualControlBase
    {
        private int _maxValue;
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

        public string _newText;
        public string NewText
        {
            get
            {
                return _newText;
            }
            set
            {
                _newText = value;
                textBox.Text = value;
            }
        }

        private int _value;



        public NumberTextBoxControl() : base()
        {
            InitializeComponent();

            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }

        protected override void ToRuntime()
        {
            base.ToRuntime();

            ValidateValue();
            UpdateTextBoxSize();
        }

        protected override void ToDesign()
        {
            base.ToDesign();

            ValidateValue();
            UpdateTextBoxSize();
        }

        private void HandleResize(object sender, EventArgs e)
        {
            UpdateTextBoxSize();
        }

        private void UpdateTextBoxSize()
        {
            float size = (Height - 8f) / 1.5f;
            size = size <= 0 ? 1 : size;

            Font font = new Font(textBox.Font.FontFamily, size, FontStyle.Regular);
            textBox.Font = font;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            if(!FBConnector.DesignMode)
            {
                int inputValue = FBConnector.GetPinInt(NumberTextBoxFB.InputToControl);
                if(inputValue != _value)
                {
                    _value = inputValue;
                    textBox.Text = _value.ToString();
                }
            }
        }

        private void UpdateState()
        {

        }



        private void HandleKeyPress(object sender, KeyPressEventArgs e)
        {
            if(!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                ValidateValue();

                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void ValidateValue()
        {
            ReadResult readResult = ReadValue(out var value);
            textBox.Text = value.ToString();
            _value = value;

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
                if(!DesignMode)
                {
                    FBConnector.SetPinValue(NumberTextBoxFB.OutputFromControl, _value);
                }
            }

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
