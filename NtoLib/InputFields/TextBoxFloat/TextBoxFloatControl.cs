using FB.VisualFB;
using NtoLib.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NtoLib.InputFields.TextBoxFloat
{
    [ComVisible(true)]
    [Guid("F7A5EFFD-CA1E-4564-BA69-C70BFAA125A5")]
    [DisplayName("Дробное поле")]
    public partial class TextBoxFloatControl : VisualControlBase
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

        private float _maxValueProperty;
        [Category("Значение")]
        [DisplayName("Максимальное значение")]
        public float MaxValueProperty

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

        private float _minValueProperty;
        [Category("Значение")]
        [DisplayName("Минимальное значение")]
        public float MinValueProperty
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

        private int _significatDigits;
        [Category("Значение")]
        [DisplayName("Доп. значащие цифры")]
        public int SignificantDigits
        {
            get
            {
                return _significatDigits;
            }
            set
            {
                _significatDigits = value;

                if(_significatDigits < 4)
                    _significatDigits = 4;
                else if(_significatDigits > 10)
                    _significatDigits = 10;

                UpdateText();
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

        private float _minValueInput;
        private float _maxValueInput;

        private float _actualMinValue;
        private float _actualMaxValue;

        private float _value;
        private bool _isInitialized = false;
        private bool _editMode = false;

        private float _lastInput;



        public TextBoxFloatControl()
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
                _maxValueInput = FBConnector.GetPinValue<float>(TextBoxFloatFB.MaxValueToControlId);
                _minValueInput = FBConnector.GetPinValue<float>(TextBoxFloatFB.MinValueToControlId);
                UpdateLimits();

                float input = FBConnector.GetPinValue<float>(TextBoxFloatFB.OutputToControlId);
                if(input != _lastInput)
                {
                    _lastInput = input;
                    _value = input;

                    UpdateText();
                }

                if(_isInitialized)
                    FBConnector.SetPinValue(TextBoxFloatFB.InputFromControlId, _value);
            }
        }



        private void HandleVisibleChanged(object sender, EventArgs e)
        {
            ToCommonMode();
        }



        private void HandleTextBoxMouseDown(object sender, MouseEventArgs e)
        {
            ToEditMode();
        }

        private void ToEditMode()
        {
            if(_editMode)
                return;

            int beforeLenght = TextBefore.Length;
            if(beforeLenght > 0)
                beforeLenght++;

            int afterLenght = TextAfter.Length;
            if(afterLenght > 0)
                afterLenght++;

            textBox.Text = textBox.Text.Substring(beforeLenght, textBox.Text.Length - beforeLenght - afterLenght);
            textBox.SelectAll();
        }

        private void ToCommonMode()
        {
            if(!_editMode)
                return;

            UpdateText();
            DropFocus();
        }

        private void DropFocus()
        {
            textBox.Enabled = false;
            textBox.Enabled = true;
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
            string format = CreateFormat(_value, _significatDigits);
            string text = _value.ToString(format);

            if(!string.IsNullOrEmpty(TextBefore))
                text = TextBefore + ' ' + text;

            if(!string.IsNullOrEmpty(TextAfter))
                text = text + ' ' + TextAfter;

            textBox.Text = text;
        }

        private string CreateFormat(float number, int significantDigit)
        {
            float absNumber = Math.Abs(number);

            int addDigits = significantDigit - 4;
            addDigits = addDigits > 0 ? addDigits : 0;
            string additionalDigits = new string('#', addDigits);

            if(absNumber == 0)
                return "0.000" + additionalDigits;
            else if(absNumber < 0.1)
                return "0.000" + additionalDigits + "e0";
            else if(absNumber < 10)
                return "0.000" + additionalDigits;
            else if(absNumber < 100)
                return "#0.00" + additionalDigits;
            else if(absNumber < 1000)
                return "##0.0" + additionalDigits;
            else if(absNumber < 10000)
                return "###0." + additionalDigits;
            else
                return "0.000" + additionalDigits + "e0";
        }

        private void ValidateValue()
        {
            ValidateValue(true);
        }

        private void ValidateValue(bool callMessages = true)
        {
            _editMode = false;

            string text = textBox.Text.Replace('.', ',');
            ReadResult readResult = ReadValue(text, out var value);

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
                    FBConnector.SetPinValue(TextBoxFloatFB.InputFromControlId, _value);
            }

            UpdateText();
            DropFocus();
        }

        private ReadResult ReadValue(string text, out float value)
        {
            value = 0;

            if(string.IsNullOrEmpty(text))
            {
                value = 0;
                return ReadResult.Success;
            }
            else if(!float.TryParse(text, out var number))
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
