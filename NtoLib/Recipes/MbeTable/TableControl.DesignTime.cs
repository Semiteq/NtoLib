using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable;

public partial class TableControl
{
	[NonSerialized] private readonly List<Func<ColorScheme, ColorScheme>> _pendingColorMutations = new();
	[NonSerialized] private Color? _blockedBgColor;
	[NonSerialized] private Color? _buttonsColor;
	[NonSerialized] private Color? _controlBgColor;
	[NonSerialized] private Color? _headerBgColor;
	[NonSerialized] private Font? _headerFont;
	[NonSerialized] private Color? _headerTextColor;
	[NonSerialized] private Color? _lineBgColor;
	[NonSerialized] private Font? _lineFont;
	[NonSerialized] private Color? _lineTextColor;
	[NonSerialized] private Color? _passedLineBgColor;
	[NonSerialized] private Font? _passedLineFont;
	[NonSerialized] private Color? _passedLineTextColor;
	[NonSerialized] private int? _rowHeight;
	[NonSerialized] private Color? _selectedLineBgColor;
	[NonSerialized] private Font? _selectedLineFont;
	[NonSerialized] private Color? _selectedLineTextColor;
	[NonSerialized] private Color? _statusBgColor;
	[NonSerialized] private Color? _tableBgColor;

	// See the https://github.com/Semiteq/NtoLib/issues/80
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

	[DisplayName("Высота строки")]
	public int RowLineHeight
	{
		get => _rowHeight ??= ColorScheme.Default.LineHeight;
		set
		{
			if (_rowHeight == value)
			{
				return;
			}

			_rowHeight = value;
			if (_table != null)
			{
				_table.RowTemplate.Height = value;
			}

			RegisterColorMutation(scheme => scheme with { LineHeight = value });
		}
	}

	[DisplayName("Цвет фона")]
	public Color ControlBgColor
	{
		get => _controlBgColor ??= ColorScheme.Default.ControlBackgroundColor;
		set
		{
			if (SetDesignTimeProperty(ref _controlBgColor, value, s => s with { ControlBackgroundColor = value }))
			{
				BackColor = value;
			}
		}
	}

	[DisplayName("Цвет статуса")]
	public Color StatusBgColor
	{
		get => _statusBgColor ??= ColorScheme.Default.StatusBgColor;
		set
		{
			if (SetDesignTimeProperty(ref _statusBgColor, value, s => s with { StatusBgColor = value }))
			{
				if (_labelStatus != null)
				{
					_labelStatus.BackColor = value;
				}
			}
		}
	}

	[DisplayName("Цвет фона таблицы")]
	public Color TableBgColor
	{
		get => _tableBgColor ??= ColorScheme.Default.TableBackgroundColor;
		set
		{
			if (SetDesignTimeProperty(ref _tableBgColor, value, s => s with { TableBackgroundColor = value }))
			{
				if (_table != null)
				{
					_table.BackgroundColor = value;
				}
			}
		}
	}

	[DisplayName("Цвет кнопок")]
	public Color ButtonsColor
	{
		get => _buttonsColor ??= ColorScheme.Default.ButtonsColor;
		set
		{
			if (!SetDesignTimeProperty(ref _buttonsColor, value,
					s => s with { ButtonsColor = value, BlockedButtonsColor = Darken(value) }))
			{
				return;
			}

			if (_buttonOpen != null)
			{
				_buttonOpen.BackColor = value;
			}

			if (_buttonSave != null)
			{
				_buttonSave.BackColor = value;
			}

			if (_buttonAddBefore != null)
			{
				_buttonAddBefore.BackColor = value;
			}

			if (_buttonAddAfter != null)
			{
				_buttonAddAfter.BackColor = value;
			}

			if (_buttonDel != null)
			{
				_buttonDel.BackColor = value;
			}

			if (_buttonWrite != null)
			{
				_buttonWrite.BackColor = value;
			}
		}
	}

	[DisplayName("Шрифт заголовка таблицы")]
	public Font HeaderFont
	{
		get => _headerFont ??= ColorScheme.Default.HeaderFont;
		set => SetDesignTimeProperty(ref _headerFont, value, s => s with { HeaderFont = value });
	}

	[DisplayName("Цвет фона заголовка таблицы")]
	public Color HeaderBgColor
	{
		get => _headerBgColor ??= ColorScheme.Default.HeaderBgColor;
		set => SetDesignTimeProperty(ref _headerBgColor, value, s => s with { HeaderBgColor = value });
	}

	[DisplayName("Цвет текста заголовка таблицы")]
	public Color HeaderTextColor
	{
		get => _headerTextColor ??= ColorScheme.Default.HeaderTextColor;
		set => SetDesignTimeProperty(ref _headerTextColor, value, s => s with { HeaderTextColor = value });
	}

	[DisplayName("Шрифт строки таблицы")]
	public Font LineFont
	{
		get => _lineFont ??= ColorScheme.Default.LineFont;
		set => SetDesignTimeProperty(ref _lineFont, value, s => s with { LineFont = value });
	}

	[DisplayName("Цвет фона строки таблицы")]
	public Color LineBgColor
	{
		get => _lineBgColor ??= ColorScheme.Default.LineBgColor;
		set => SetDesignTimeProperty(ref _lineBgColor, value, s => s with { LineBgColor = value });
	}

	[DisplayName("Цвет текста строки таблицы")]
	public Color LineTextColor
	{
		get => _lineTextColor ??= ColorScheme.Default.LineTextColor;
		set => SetDesignTimeProperty(ref _lineTextColor, value, s => s with { LineTextColor = value });
	}

	[DisplayName("Шрифт текущей строки таблицы")]
	public Font SelectedLineFont
	{
		get => _selectedLineFont ??= ColorScheme.Default.SelectedLineFont;
		set => SetDesignTimeProperty(ref _selectedLineFont, value, s => s with { SelectedLineFont = value });
	}

	[DisplayName("Цвет фона текущей строки таблицы")]
	public Color SelectedLineBgColor
	{
		get => _selectedLineBgColor ??= ColorScheme.Default.SelectedLineBgColor;
		set => SetDesignTimeProperty(ref _selectedLineBgColor, value, s => s with { SelectedLineBgColor = value });
	}

	[DisplayName("Цвет текста текущей строки таблицы")]
	public Color SelectedLineTextColor
	{
		get => _selectedLineTextColor ??= ColorScheme.Default.SelectedLineTextColor;
		set => SetDesignTimeProperty(ref _selectedLineTextColor, value, s => s with { SelectedLineTextColor = value });
	}

	[DisplayName("Шрифт пройденной строки таблицы")]
	public Font PassedLineFont
	{
		get => _passedLineFont ??= ColorScheme.Default.PassedLineFont;
		set => SetDesignTimeProperty(ref _passedLineFont, value, s => s with { PassedLineFont = value });
	}

	[DisplayName("Цвет фона пройденной строки таблицы")]
	public Color PassedLineBgColor
	{
		get => _passedLineBgColor ??= ColorScheme.Default.PassedLineBgColor;
		set => SetDesignTimeProperty(ref _passedLineBgColor, value, s => s with { PassedLineBgColor = value });
	}

	[DisplayName("Цвет текста пройденной строки таблицы")]
	public Color PassedLineTextColor
	{
		get => _passedLineTextColor ??= ColorScheme.Default.PassedLineTextColor;
		set => SetDesignTimeProperty(ref _passedLineTextColor, value, s => s with { PassedLineTextColor = value });
	}

	[DisplayName("Цвет фона заблокированной ячейки")]
	public Color BlockedBgColor
	{
		get => _blockedBgColor ??= ColorScheme.Default.BlockedBgColor;
		set => SetDesignTimeProperty(ref _blockedBgColor, value, s => s with { BlockedBgColor = value });
	}

	private bool SetDesignTimeProperty<T>(ref T? field, T value, Func<ColorScheme, ColorScheme> mutation)
	{
		if (EqualityComparer<T>.Default.Equals(field!, value!))
		{
			return false;
		}

		field = value;
		RegisterColorMutation(mutation);

		return true;
	}

	private void RegisterColorMutation(Func<ColorScheme, ColorScheme> mutation)
	{
		if (_colorSchemeProvider != null)
		{
			_colorSchemeProvider.Mutate(mutation);
		}
		else
		{
			_pendingColorMutations.Add(mutation);
		}
	}

	private void ApplyPendingColorMutations()
	{
		if (_pendingColorMutations.Count == 0)
		{
			return;
		}

		if (_colorSchemeProvider == null)
		{
			return;
		}

		_colorSchemeProvider.Mutate(scheme =>
		{
			var result = scheme;
			foreach (var mutation in _pendingColorMutations)
			{
				result = mutation(result);
			}

			return result;
		});

		_pendingColorMutations.Clear();
	}
}
