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
        [DispId(10)]
        [Category("Внешний вид")]
        [DisplayName("Цвет границы")]
        public override Color BackColor { get; set; } = Color.Black;

        private int _borderWidth = 1;
        [DispId(11)]
        [Category("Внешний вид")]
        [DisplayName("Толщина границы")]
        public int BorderWidth
        {
            get
            {
                return _borderWidth;
            }
            set
            {
                _borderWidth = value < 0 ? 0 : value;
                UpdateBorderWidth();
                UpdateFont();
            }
        }

        private bool _userLock;
        [DispId(20)]
        [Category("Внешний вид")]
        [DisplayName("Блокировка ввода")]
        public bool UserLock
        {
            get
            {
                return _userLock;
            }
            set
            {
                _userLock = value;
                UpdateLockBehaviour();
            }
        }

        private Color _backColorUnlocked = Color.White;
        [DispId(30)]
        [Category("Внешний вид")]
        [DisplayName("Цвет разблокированного")]
        public Color BackColorUnlocked
        {
            get
            {
                return _backColorUnlocked;
            }
            set
            {
                _backColorUnlocked = value;
                UpdateLockBehaviour(true);
            }
        }

        private Color _backColorLocked = Color.LightYellow;
        [DispId(40)]
        [Category("Внешний вид")]
        [DisplayName("Цвет заблокированного")]
        public Color BackColorLocked
        {
            get
            {
                return _backColorLocked;
            }
            set
            {
                _backColorLocked = value;
                UpdateLockBehaviour(true);
            }
        }

        private string _textBefore;
        [DispId(50)]
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
        [DispId(60)]
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

        [DispId(70)]
        [Category("Внешний вид")]
        [DisplayName("Выравнивание")]
        public HorizontalAlignment Alignment
        {
            get
            {
                return textBox.TextAlign;
            }
            set
            {
                textBox.TextAlign = value;

                if(value == HorizontalAlignment.Left)
                    label.TextAlign = ContentAlignment.MiddleLeft;
                else if(value == HorizontalAlignment.Right)
                    label.TextAlign = ContentAlignment.MiddleRight;
                else
                    label.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        private bool _boldFont = true;
        [DispId(75)]
        [Category("Внешний вид")]
        [DisplayName("Жирный шрифт")]
        public bool BoldFont
        {
            get
            {
                return _boldFont;
            }
            set
            {
                _boldFont = value;
                UpdateFont();
            }
        }

        private int _decimalPoint = 2;
        [DispId(80)]
        [Category("Внешний вид")]
        [DisplayName("Цифр после запятой")]
        public int DecimalPoint
        {
            get
            {
                return _decimalPoint;
            }
            set
            {
                _decimalPoint = value;

                if(_decimalPoint < 0)
                    _decimalPoint = 0;
                else if(_decimalPoint > 10)
                    _decimalPoint = 10;

                _stringFormat = CreateFormat(_decimalPoint);
                UpdateText();
            }
        }

        public bool _exponentialForm;
        [DispId(90)]
        [Category("Внешний вид")]
        [DisplayName("Экспоненциальный вид")]
        public bool ExponentialForm
        {
            get
            {
                return _exponentialForm;
            }
            set
            {
                _exponentialForm = value;

                _stringFormat = CreateFormat(DecimalPoint);
                UpdateText();
            }
        }

        [DispId(100)]
        [Category("Значение")]
        [DisplayName("Границы из контрола")]
        [Description("Ограничивать вводимое значение пределами, заданными в свойстах контрола (таблица выше)")]
        public bool UseLimitsFromUI { get; set; }

        private float _maxValueProperty;
        [DispId(120)]
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
        [DispId(130)]
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

        /// <summary>
        /// Свойство, необходимое для сохранения настроек
        /// шрифта между перезапусками средствами MasterSCADA
        /// </summary>
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
                label.Font = value;
            }
        }

        private string _stringFormat;

        /// <summary> Отступ от границы до TextBox.
        /// Нужен для того, чтобы символы не "прилипали к границе" </summary>
        private const int _textBoxOffset = 3;

        /// <summary> Нижний порог значения, полученный с входа ФБ </summary>
        private float _minValueInput;
        /// <summary> Верхний порог значения, полученны с входа ФБ </summary>
        private float _maxValueInput;

        /// <summary> Текущий нижний порог значения, учитывающий как вход ФБ, 
        /// так и свойство контрола </summary>
        private float _actualMinValue;
        /// <summary> Текущий верхний порог значения, учитывающий как вход ФБ,
        /// так и свойство контрола </summary>
        private float _actualMaxValue;

        /// <summary> Последнее отображаемое значение </summary>
        private float _value;
        private bool _isInitialized = false;
        private bool _editMode = false;

        /// <summary> Переменная требуемого состояния блокировки ввода,
        /// полученная с входа ФБ </summary>
        private bool _inputLock;
        /// <summary> Текущее состояние блокировки ввода </summary>
        private bool _isLocked = false;

        /// <summary> Поледнее значение, полученное из ФБ </summary>
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
            UpdateBorderWidth();
            UpdateFont();
            UpdateLockBehaviour(true);
            ValidateValue(true);
            textBox.ValidatingValue += ValidateValue;

            FocusManager.Focused += UpdateFocus;
        }

        protected override void ToDesign()
        {
            base.ToDesign();

            _isInitialized = false;

            UpdateBorderWidth();
            UpdateFont();
            UpdateLockBehaviour(true);
            ValidateValue(true);
            textBox.ValidatingValue -= ValidateValue;

            FocusManager.Focused -= UpdateFocus;
        }

        private void HandleResize(object sender, EventArgs e)
        {
            UpdateFont();
        }

        private void HandleVisibleChanged(object sender, EventArgs e)
        {
            ToCommonMode();
        }

        private void UpdateFocus(VisualControlBase focusedControl)
        {
            try
            {
                if (IsDisposed)
                    return;
                    
                if (this != focusedControl)
                    ToCommonMode();
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем исключения для освобожденных объектов
            }
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

                _inputLock = FBConnector.GetPinValue<bool>(TextBoxFloatFB.LockToControl);
                UpdateLockBehaviour();

                if(_isInitialized)
                    FBConnector.SetPinValue(TextBoxFloatFB.InputFromControlId, _value);
            }
        }



        private void UpdateFont()
        {
            int doubledBorder = 2 * BorderWidth;
            int doubledOffset = 2 * _textBoxOffset;

            float size = (Height - doubledBorder - doubledOffset - 1f) / 1.525f;
            size = size <= 1 ? 1 : size;

            FontStyle style = _boldFont ? FontStyle.Bold : FontStyle.Regular;

            Font font = new Font(TextBoxFont.FontFamily, size, style);
            TextBoxFont = font;
        }

        private void UpdateBorderWidth()
        {
            int doubledBorder = 2 * BorderWidth;
            int doubledOffset = 2 * _textBoxOffset;

            pictureBox.Location = new Point(BorderWidth, BorderWidth);
            pictureBox.Size = new Size(Width - doubledBorder, Height - doubledBorder);

            textBox.Location = new Point(BorderWidth + 3, BorderWidth + 3);
            textBox.Size = new Size(Width - doubledBorder - doubledOffset, Height - doubledBorder - doubledOffset);

            label.Location = new Point(BorderWidth, BorderWidth + 3);
            label.Size = new Size(Width - doubledBorder, Height - doubledBorder - doubledOffset);
        }

        private void UpdateLockBehaviour(bool forceUpdate = false)
        {
            bool actualIsLocked = _inputLock || UserLock;
            if((actualIsLocked == _isLocked) && !forceUpdate)
                return;

            if(actualIsLocked)
            {
                textBox.BackColor = BackColorLocked;
                label.BackColor = BackColorLocked;
                pictureBox.BackColor = BackColorLocked;

                textBox.Enabled = false;
                label.Visible = true;
                _isLocked = true;
            }
            else
            {
                textBox.BackColor = BackColorUnlocked;
                label.BackColor = BackColorUnlocked;
                pictureBox.BackColor = BackColorUnlocked;

                textBox.Enabled = true;
                label.Visible = false;
                _isLocked = false;
            }
        }

        private void ToEditMode(object sender, MouseEventArgs e)
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
            textBox.Focus();
            textBox.SelectAll();

            FocusManager.OnFocused(this);
            _editMode = true;
        }

        private void ToCommonMode()
        {
            if(!_editMode)
                return;

            UpdateText();
            DropFocus();

            _editMode = false;
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
            string text = _value.ToString(_stringFormat);

            if(!string.IsNullOrEmpty(TextBefore))
                text = TextBefore + ' ' + text;

            if(!string.IsNullOrEmpty(TextAfter))
                text = text + ' ' + TextAfter;

            textBox.Text = text;
            label.Text = text;
        }

        private string CreateFormat(int decimalPoints)
        {
            string format = "0." + new string('0', decimalPoints);

            if(ExponentialForm)
                format += "e0";

            return format;
        }

        private void ValidateValue()
        {
            ValidateValue(false);
        }

        private void ValidateValue(bool supressMessages = false)
        {
            string text = textBox.Text.Replace('.', ',');
            ReadResult readResult = ReadValue(text, out var value);

            if(readResult != ReadResult.Success)
            {
                if(!supressMessages)
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

            ToCommonMode();
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
