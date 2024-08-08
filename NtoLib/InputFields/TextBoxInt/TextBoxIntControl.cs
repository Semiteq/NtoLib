using FB.VisualFB;
using NtoLib.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.InputFields.TextBoxInt
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

        [Category("Значение")]
        [DisplayName("Границы из контрола")]
        [Description("Переключает ограничение вводимого значения пределами ниже")]
        public bool UseLimitsFromUI { get; set; }

        private int _maxValueProperty;
        [Category("Значение")]
        [DisplayName("Максимальное значение")]
        public int MaxValueProperty

        {
            get
            {
                return _maxValueProperty;
            }
            set
            {
                _maxValueProperty = value;
                if(_maxValueProperty < _minValueProperty)
                    _maxValueProperty = _minValueProperty;
            }
        }

        private int _minValueProperty;
        [Category("Значение")]
        [DisplayName("Минимальное значение")]
        public int MinValueProperty
        {
            get
            {
                return _minValueProperty;
            }
            set
            {
                _minValueProperty = value;
                if(_minValueProperty > _maxValueProperty)
                    _minValueProperty = MaxValueProperty;
            }
        }

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

        private int _minValueInput;
        private int _maxValueInput;

        private int _actualMinValue;
        private int _actualMaxValue;

        private int _value;
        private bool _isInitialized = false;
        private bool _editMode = false;

        private int _lastInput;



        public TextBoxIntControl()
        {
            InitializeComponent();
        }



        protected override void ToRuntime()
        {
            base.ToRuntime();

            _isInitialized = true;

            UpdateLimits();
            UpdateTextBoxFontSize();
            ValidateValue(false);
            textBox.ValidatingValue += ValidateValue;
        }

        protected override void ToDesign()
        {
            base.ToDesign();

            _isInitialized = false;

            UpdateTextBoxFontSize();
            ValidateValue(false);
            textBox.ValidatingValue -= ValidateValue;
        }

        private void HandleResize(object sender, EventArgs e)
        {
            UpdateTextBoxFontSize();
        }

        private void UpdateTextBoxFontSize()
        {
            float size = (Height - 8f - 1f) / 1.525f;
            size = size <= 1 ? 1 : size;

            Font font = new Font(textBox.Font.FontFamily, size, FontStyle.Regular);
            textBox.Font = font;
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if(!DesignMode)
            {
                _maxValueInput = FBConnector.GetPinInt(TextBoxIntFB.MaxValueToControlId);
                _minValueInput = FBConnector.GetPinInt(TextBoxIntFB.MinValueToControlId);
                UpdateLimits();

                int input = FBConnector.GetPinInt(TextBoxIntFB.OutputToControlId);
                if(input != _lastInput)
                {
                    _lastInput = input;
                    _value = input;

                    UpdateText();
                }

                if(_isInitialized)
                    FBConnector.SetPinValue(TextBoxIntFB.InputFromControlId, _value);
            }
        }



        private void HandleTextBoxMouseDown(object sender, MouseEventArgs e)
        {
            ToEditMode();
        }

        private void ToEditMode()
        {
            if(!_editMode)
            {
                int beforeLenght = TextBefore.Length;
                if(beforeLenght > 0)
                    beforeLenght++;

                int afterLenght = TextAfter.Length;
                if(afterLenght > 0)
                    afterLenght++;

                textBox.Text = textBox.Text.Substring(beforeLenght, textBox.Text.Length - beforeLenght - afterLenght);
                textBox.SelectAll();
            }

            _editMode = true;
        }



        private void UpdateLimits()
        {
            if(UseLimitsFromUI)
            {
                _actualMaxValue = Math.Min(_maxValueInput, MaxValueProperty);
                _actualMinValue = Math.Max(_minValueInput, MinValueProperty);
            }
            else
            {
                _actualMaxValue = _maxValueInput;
                _actualMinValue = _minValueInput;
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
            ValidateValue(true);
        }

        private void ValidateValue(bool callMessages = true)
        {
            _editMode = false;

            ReadResult readResult = ReadValue(textBox.Text, out var value);

            if(readResult != ReadResult.Success)
            {
                if(callMessages)
                {
                    string message;
                    switch(readResult)
                    {
                        case ReadResult.AboveMax:
                        {
                            message = $"Значение должно быть ≤ {_actualMaxValue}";
                            break;
                        }
                        case ReadResult.BelowMin:
                        {
                            message = $"Значение должно быть ≥ {_actualMinValue}";
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

        private ReadResult ReadValue(string text, out int value)
        {
            value = 0;

            if(string.IsNullOrEmpty(text))
            {
                value = 0;
                return ReadResult.Success;
            }
            else if(!int.TryParse(text, out var number))
            {
                return ReadResult.ParseError;
            }
            else if(number < _actualMinValue)
            {
                return ReadResult.BelowMin;
            }
            else if(number > _actualMaxValue)
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
