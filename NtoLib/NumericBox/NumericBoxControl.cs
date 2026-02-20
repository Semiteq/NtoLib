using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using FB.VisualFB;

using FluentResults;

using NtoLib.NumericBox.Helpers;

using ContentAlignment = System.Drawing.ContentAlignment;

namespace NtoLib.NumericBox;

[ComVisible(true)]
[Guid("E44A8182-8463-4D07-977D-FE7C993F76CD")]
[DisplayName("Поле ввода")]
public partial class NumericBoxControl : VisualControlBase
{
	private const int TextBoxOffset = 3;
	private const int RedrawIntervalMs = 50;
	private const float ValuePrecision = 0.001f;
	private bool _editMode;

	private bool _isInitialized;
	private bool _pinLock;

	private Timer? _redrawTimer;

	private string _stringFormat;

	private float _value;

	public NumericBoxControl()
	{
		InitializeComponent();
		_stringFormat = _displayFormat.ToFormatString();
	}

	protected override void ToRuntime()
	{
		base.ToRuntime();

		_isInitialized = true;

		UpdateBorderWidth();
		UpdateFont();
		UpdateLockBehaviour();
		textBox.ValidatingValue += ReadAndValidateValue;

		FocusManager.Focused += UpdateFocus;

		_redrawTimer = new Timer { Interval = RedrawIntervalMs };
		_redrawTimer.Tick += PollFbState;
		_redrawTimer.Start();
	}

	protected override void ToDesign()
	{
		_redrawTimer?.Stop();
		_redrawTimer?.Dispose();
		_redrawTimer = null;

		base.ToDesign();

		_isInitialized = false;

		UpdateBorderWidth();
		UpdateFont();
		UpdateLockBehaviour();
		textBox.ValidatingValue -= ReadAndValidateValue;

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
		if (this != focusedControl)
		{
			ToCommonMode();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
	}

	private void PollFbState(object? sender, EventArgs e)
	{
		if (DesignMode
			|| FBConnector.DesignMode
			|| FBConnector?.Fb is not NumericBoxFB fb
			|| !_isInitialized)
		{
			return;
		}

		var newValue = fb.OutputValue;
		var newLock = fb.IsLocked;

		var valueChanged = Math.Abs(newValue - _value) > ValuePrecision;
		var lockChanged = newLock != _pinLock;

		if (valueChanged)
		{
			_value = newValue;

			if (!_editMode)
			{
				UpdateText();
			}
		}

		if (lockChanged)
		{
			_pinLock = newLock;
			UpdateLockBehaviour();
		}
	}

	private void UpdateFont()
	{
		float size;

		if (_fontMode == FontSizeMode.Fixed)
		{
			size = _fixedFontSize;
		}
		else
		{
			var doubledBorder = 2 * BorderWidth;
			var doubledOffset = 2 * TextBoxOffset;

			size = (Height - doubledBorder - doubledOffset - 1f) / 1.525f;
			size = size <= 1 ? 1 : size;
		}

		var style = _boldFont ? FontStyle.Bold : FontStyle.Regular;

		var font = new Font(TextBoxFont.FontFamily, size, style);
		TextBoxFont = font;
	}

	private void UpdateBorderWidth()
	{
		var doubledBorder = 2 * BorderWidth;
		var doubledOffset = 2 * TextBoxOffset;

		pictureBox.Location = new Point(BorderWidth, BorderWidth);
		pictureBox.Size = new Size(Width - doubledBorder, Height - doubledBorder);

		textBox.Location = new Point(BorderWidth + 3, BorderWidth + 3);
		textBox.Size = new Size(Width - doubledBorder - doubledOffset, Height - doubledBorder - doubledOffset);

		label.Location = new Point(BorderWidth, BorderWidth + 3);
		label.Size = new Size(Width - doubledBorder, Height - doubledBorder - doubledOffset);
	}

	private void UpdateLockBehaviour()
	{
		var actualIsLocked = _pinLock || UserLock;

		if (actualIsLocked)
		{
			textBox.BackColor = BackColorLocked;
			label.BackColor = BackColorLocked;
			pictureBox.BackColor = BackColorLocked;

			textBox.Enabled = false;
			label.Visible = true;
		}
		else
		{
			textBox.BackColor = BackColorUnlocked;
			label.BackColor = BackColorUnlocked;
			pictureBox.BackColor = BackColorUnlocked;

			textBox.Enabled = true;
			label.Visible = false;
		}
	}

	private void ToEditMode(object sender, MouseEventArgs e)
	{
		if (_editMode)
		{
			return;
		}

		var beforeLength = TextBefore.Length;
		if (beforeLength > 0)
		{
			beforeLength++;
		}

		var afterLength = TextAfter.Length;
		if (afterLength > 0)
		{
			afterLength++;
		}

		textBox.Text = textBox.Text.Substring(beforeLength, textBox.Text.Length - beforeLength - afterLength);
		textBox.Focus();
		textBox.SelectAll();

		FocusManager.OnFocused(this);
		_editMode = true;
	}

	private void ToCommonMode()
	{
		if (!_editMode)
		{
			return;
		}

		UpdateText();
		DropFocus();

		_editMode = false;
	}

	private void DropFocus()
	{
		textBox.Enabled = false;
		textBox.Enabled = true;
	}

	private void UpdateText()
	{
		var text = _value.ToString(_stringFormat);

		if (!string.IsNullOrEmpty(TextBefore))
		{
			text = TextBefore + ' ' + text;
		}

		if (!string.IsNullOrEmpty(TextAfter))
		{
			text = text + ' ' + TextAfter;
		}

		textBox.Text = text;
		label.Text = text;
	}

	private void ReadAndValidateValue()
	{
		if (DesignMode
			|| FBConnector.DesignMode
			|| FBConnector?.Fb is not NumericBoxFB fb
			|| !_isInitialized)
		{
			return;
		}

		var text = textBox.Text.Replace('.', ',');
		var readResult = ReadValue(text);
		if (readResult.IsFailed)
		{
			var message = readResult.Reasons.FirstOrDefault()?.Message;
			message ??= "Ошибка при распознавании числа";
			MessageBox.Show(message, @"MasterSCADA", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			ToCommonMode();

			return;
		}

		var value = readResult.Value;
		var validationResult = fb.ValidateValue(value, _stringFormat);
		if (validationResult.IsFailed)
		{
			var message = validationResult.Reasons.FirstOrDefault()?.Message;
			message ??= "Недопустимое значение";
			MessageBox.Show(message, @"MasterSCADA", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			ToCommonMode();

			return;
		}

		_value = value;
		ToCommonMode();
	}

	private Result<float> ReadValue(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		if (!float.TryParse(text, out var number))
		{
			return new Error("Невозможно распознать число");
		}

		return number;
	}

	#region Visual properties

	[DispId(10)]
	[Category("Внешний вид")]
	[DisplayName("Цвет границы")]
	public override Color BackColor
	{
		get => base.BackColor;
		set
		{
			if (value == Color.Transparent)
			{
				return;
			}

			base.BackColor = value;
		}
	}

	private int _borderWidth = 1;

	[DispId(11)]
	[Category("Внешний вид")]
	[DisplayName("Толщина границы")]
	public int BorderWidth
	{
		get => _borderWidth;
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
		get => _userLock;
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
		get => _backColorUnlocked;
		set
		{
			if (_backColorUnlocked == Color.Transparent)
			{
				return;
			}

			_backColorUnlocked = value;
			UpdateLockBehaviour();
		}
	}

	private Color _backColorLocked = Color.LightYellow;

	[DispId(40)]
	[Category("Внешний вид")]
	[DisplayName("Цвет заблокированного")]
	public Color BackColorLocked
	{
		get => _backColorLocked;
		set
		{
			if (_backColorLocked == Color.Transparent)
			{
				return;
			}

			_backColorLocked = value;
			UpdateLockBehaviour();
		}
	}

	private string _textBefore = "";

	[DispId(50)]
	[Category("Внешний вид")]
	[DisplayName("Текст до")]
	public string TextBefore
	{
		get => _textBefore;
		set
		{
			_textBefore = value;
			UpdateText();
		}
	}

	private string _textAfter = "";

	[DispId(60)]
	[Category("Внешний вид")]
	[DisplayName("Текст после")]
	public string TextAfter
	{
		get => _textAfter;
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
		get => textBox.TextAlign;
		set
		{
			textBox.TextAlign = value;

			label.TextAlign = value switch
			{
				HorizontalAlignment.Left => ContentAlignment.MiddleLeft,
				HorizontalAlignment.Right => ContentAlignment.MiddleRight,
				_ => ContentAlignment.MiddleCenter
			};
		}
	}

	private bool _boldFont = true;

	[DispId(75)]
	[Category("Внешний вид")]
	[DisplayName("Жирный шрифт")]
	public bool BoldFont
	{
		get => _boldFont;
		set
		{
			_boldFont = value;
			UpdateFont();
		}
	}

	private DisplayFormat _displayFormat = DisplayFormat.TwoDecimals;

	[DispId(80)]
	[Category("Внешний вид")]
	[DisplayName("Формат отображения")]
	public DisplayFormat DisplayFormat
	{
		get => _displayFormat;
		set
		{
			_displayFormat = value;
			_stringFormat = value.ToFormatString();
			UpdateText();
		}
	}

	private FontSizeMode _fontMode = FontSizeMode.Auto;

	[DispId(95)]
	[Category("Внешний вид")]
	[DisplayName("Режим размера шрифта")]
	public FontSizeMode FontMode
	{
		get => _fontMode;
		set
		{
			_fontMode = value;
			UpdateFont();
		}
	}

	private float _fixedFontSize = 12f;

	[DispId(96)]
	[Category("Внешний вид")]
	[DisplayName("Фиксированный размер шрифта")]
	public float FixedFontSize
	{
		get => _fixedFontSize;
		set
		{
			_fixedFontSize = value <= 1 ? 1 : value;
			UpdateFont();
		}
	}

	/// <summary>
	/// Required for persisting font settings across restarts by MasterSCADA
	/// </summary>
	[Browsable(false)]
	public Font TextBoxFont
	{
		get => textBox.Font;
		set
		{
			textBox.Font = value;
			label.Font = value;
		}
	}

	#endregion
}
