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
                UpdateFontSize();
            }
        }

        private bool _userLock;
        [DispId(20)]
        [Category("Поведение")]
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

        [DispId(80)]
        [Category("Значение")]
        [DisplayName("Границы из контрола")]
        [Description("Переключает ограничение вводимого значения пределами ниже")]
        public bool UseLimitsFromUI { get; set; }

        private int _maxValueProperty;
        [DispId(90)]
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
        [DispId(100)]
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

        /// <summary> Отступ от границы до TextBox.
        /// Нужен для того, чтобы символы не "прилипали к границе" </summary>
        private const int _textBoxOffset = 3;

        /// <summary> Нижний порог значения, полученный с входа ФБ </summary>
        private int _minValueInput;
        /// <summary> Верхний порог значения, полученны с входа ФБ </summary>
        private int _maxValueInput;

        /// <summary> Текущий нижний порог значения, учитывающий как вход ФБ, 
        /// так и свойство контрола </summary>
        private int _actualMinValue;
        /// <summary> Текущий верхний порог значения, учитывающий как вход ФБ,
        /// так и свойство контрола </summary>
        private int _actualMaxValue;

        /// <summary> Последнее отображаемое значение </summary>
        private int _value;
        private bool _isInitialized = false;
        private bool _editMode = false;

        /// <summary> Переменная требуемого состояния блокировки ввода,
        /// полученная с входа ФБ </summary>
        private bool _inputIsLock;
        /// <summary> Текущее состояние блокировки ввода </summary>
        private bool _isLocked = false;

        /// <summary> Поледнее значение, полученное из ФБ </summary>
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
            UpdateBorderWidth();
            UpdateFontSize();
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
            UpdateFontSize();
            UpdateLockBehaviour(true);
            ValidateValue(true);
            textBox.ValidatingValue -= ValidateValue;

            FocusManager.Focused -= UpdateFocus;
        }

        private void HandleResize(object sender, EventArgs e)
        {
            UpdateFontSize();
        }

        private void HandleVisibleChanged(object sender, EventArgs e)
        {
            ToCommonMode();
        }

        private void UpdateFocus(VisualControlBase focusedControl)
        {
            if(this != focusedControl)
                ToCommonMode();
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

                _inputIsLock = FBConnector.GetPinValue<bool>(TextBoxIntFB.LockToControl);
                UpdateLockBehaviour();

                if(_isInitialized)
                    FBConnector.SetPinValue(TextBoxIntFB.InputFromControlId, _value);
            }
        }



        private void UpdateFontSize()
        {
            int doubledBorder = 2 * BorderWidth;
            int doubledOffset = 2 * _textBoxOffset;

            float size = (Height - doubledBorder - doubledOffset - 1f) / 1.525f;
            size = size <= 1 ? 1 : size;

            Font font = new Font(TextBoxFont.FontFamily, size, FontStyle.Regular);
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
            bool actualIsLocked = _inputIsLock || UserLock;
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
            string text = _value.ToString();

            if(!string.IsNullOrEmpty(TextBefore))
                text = TextBefore + ' ' + text;

            if(!string.IsNullOrEmpty(TextAfter))
                text = text + ' ' + TextAfter;

            textBox.Text = text;
            label.Text = text;
        }

        private void ValidateValue()
        {
            ValidateValue(false);
        }

        private void ValidateValue(bool supressMessages = false)
        {
            ReadResult readResult = ReadValue(textBox.Text, out var value);

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
                    FBConnector.SetPinValue(TextBoxIntFB.InputFromControlId, _value);
            }

            ToCommonMode();
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
