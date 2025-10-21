using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable;

/// <summary>
/// Design-time properties for MasterSCADA visual designer.
/// All mutations are queued until runtime initialization.
/// </summary>
public partial class TableControl
{
    [NonSerialized] private Color? _controlBgColor;
    [NonSerialized] private Color? _tableBgColor;
    [NonSerialized] private Font? _headerFont;
    [NonSerialized] private Color? _headerTextColor;
    [NonSerialized] private Color? _headerBgColor;
    [NonSerialized] private Font? _lineFont;
    [NonSerialized] private Color? _lineTextColor;
    [NonSerialized] private Color? _lineBgColor;
    [NonSerialized] private Font? _selectedLineFont;
    [NonSerialized] private Color? _selectedLineTextColor;
    [NonSerialized] private Color? _selectedLineBgColor;
    [NonSerialized] private Font? _passedLineFont;
    [NonSerialized] private Color? _passedLineTextColor;
    [NonSerialized] private Color? _passedLineBgColor;
    [NonSerialized] private Color? _blockedBgColor;
    [NonSerialized] private Color? _buttonsColor;
    [NonSerialized] private int? _rowHeight;
    [NonSerialized] private Color? _statusBgColor;

    [NonSerialized] private readonly List<Func<ColorScheme, ColorScheme>> _pendingColorMutations = new();

    [DisplayName("Высота строки")]
    public int RowLineHeight
    {
        get => _rowHeight ??= ColorScheme.Default.LineHeight;
        set
        {
            if (_rowHeight == value) return;
            _rowHeight = value;
            if (_table != null) _table.RowTemplate.Height = value;
            RegisterColorMutation(scheme => scheme with { LineHeight = value });
        }
    }

    [DisplayName("Цвет фона")]
    public Color ControlBgColor
    {
        get => _controlBgColor ??= ColorScheme.Default.ControlBackgroundColor;
        set
        {
            if (_controlBgColor == value) return;
            _controlBgColor = value;
            BackColor = value;
            RegisterColorMutation(scheme => scheme with { ControlBackgroundColor = value });
        }
    }

    [DisplayName("Цвет статуса")]
    public Color StatusBgColor
    {
        get => _statusBgColor ??= ColorScheme.Default.StatusBgColor;
        set
        {
            if (_statusBgColor == value) return;
            _statusBgColor = value;
            if (_labelStatus != null) _labelStatus.BackColor = value;
            RegisterColorMutation(scheme => scheme with { StatusBgColor = value });
        }
    }

    [DisplayName("Цвет фона таблицы")]
    public Color TableBgColor
    {
        get => _tableBgColor ??= ColorScheme.Default.TableBackgroundColor;
        set
        {
            if (_tableBgColor == value) return;
            _tableBgColor = value;
            if (_table != null) _table.BackgroundColor = value;
            RegisterColorMutation(scheme => scheme with { TableBackgroundColor = value });
        }
    }

    [DisplayName("Цвет кнопок")]
    public Color ButtonsColor
    {
        get => _buttonsColor ??= ColorScheme.Default.ButtonsColor;
        set
        {
            if (_buttonsColor == value) return;
            _buttonsColor = value;
            if (_buttonOpen != null) _buttonOpen.BackColor = value;
            if (_buttonSave != null) _buttonSave.BackColor = value;
            if (_buttonAddBefore != null) _buttonAddBefore.BackColor = value;
            if (_buttonAddAfter != null) _buttonAddAfter.BackColor = value;
            if (_buttonDel != null) _buttonDel.BackColor = value;
            if (_buttonWrite != null) _buttonWrite.BackColor = value;
            RegisterColorMutation(scheme => scheme with
            {
                ButtonsColor = value,
                BlockedButtonsColor = Darken(value)
            });
        }
    }

    [DisplayName("Шрифт заголовка таблицы")]
    public Font HeaderFont
    {
        get => _headerFont ??= ColorScheme.Default.HeaderFont;
        set
        {
            if (Equals(_headerFont, value)) return;
            _headerFont = value;
            RegisterColorMutation(scheme => scheme with { HeaderFont = value });
        }
    }

    [DisplayName("Цвет фона заголовка таблицы")]
    public Color HeaderBgColor
    {
        get => _headerBgColor ??= ColorScheme.Default.HeaderBgColor;
        set
        {
            if (_headerBgColor == value) return;
            _headerBgColor = value;
            RegisterColorMutation(scheme => scheme with { HeaderBgColor = value });
        }
    }

    [DisplayName("Цвет текста заголовка таблицы")]
    public Color HeaderTextColor
    {
        get => _headerTextColor ??= ColorScheme.Default.HeaderTextColor;
        set
        {
            if (_headerTextColor == value) return;
            _headerTextColor = value;
            RegisterColorMutation(scheme => scheme with { HeaderTextColor = value });
        }
    }

    [DisplayName("Шрифт строки таблицы")]
    public Font LineFont
    {
        get => _lineFont ??= ColorScheme.Default.LineFont;
        set
        {
            if (Equals(_lineFont, value)) return;
            _lineFont = value;
            RegisterColorMutation(scheme => scheme with { LineFont = value });
        }
    }

    [DisplayName("Цвет фона строки таблицы")]
    public Color LineBgColor
    {
        get => _lineBgColor ??= ColorScheme.Default.LineBgColor;
        set
        {
            if (_lineBgColor == value) return;
            _lineBgColor = value;
            RegisterColorMutation(scheme => scheme with { LineBgColor = value });
        }
    }

    [DisplayName("Цвет текста строки таблицы")]
    public Color LineTextColor
    {
        get => _lineTextColor ??= ColorScheme.Default.LineTextColor;
        set
        {
            if (_lineTextColor == value) return;
            _lineTextColor = value;
            RegisterColorMutation(scheme => scheme with { LineTextColor = value });
        }
    }

    [DisplayName("Шрифт текущей строки таблицы")]
    public Font SelectedLineFont
    {
        get => _selectedLineFont ??= ColorScheme.Default.SelectedLineFont;
        set
        {
            if (Equals(_selectedLineFont, value)) return;
            _selectedLineFont = value;
            RegisterColorMutation(scheme => scheme with { SelectedLineFont = value });
        }
    }

    [DisplayName("Цвет фона текущей строки таблицы")]
    public Color SelectedLineBgColor
    {
        get => _selectedLineBgColor ??= ColorScheme.Default.SelectedLineBgColor;
        set
        {
            if (_selectedLineBgColor == value) return;
            _selectedLineBgColor = value;
            RegisterColorMutation(scheme => scheme with { SelectedLineBgColor = value });
        }
    }

    [DisplayName("Цвет текста текущей строки таблицы")]
    public Color SelectedLineTextColor
    {
        get => _selectedLineTextColor ??= ColorScheme.Default.SelectedLineTextColor;
        set
        {
            if (_selectedLineTextColor == value) return;
            _selectedLineTextColor = value;
            RegisterColorMutation(scheme => scheme with { SelectedLineTextColor = value });
        }
    }

    [DisplayName("Шрифт пройденной строки таблицы")]
    public Font PassedLineFont
    {
        get => _passedLineFont ??= ColorScheme.Default.PassedLineFont;
        set
        {
            if (Equals(_passedLineFont, value)) return;
            _passedLineFont = value;
            RegisterColorMutation(scheme => scheme with { PassedLineFont = value });
        }
    }

    [DisplayName("Цвет фона пройденной строки таблицы")]
    public Color PassedLineBgColor
    {
        get => _passedLineBgColor ??= ColorScheme.Default.PassedLineBgColor;
        set
        {
            if (_passedLineBgColor == value) return;
            _passedLineBgColor = value;
            RegisterColorMutation(scheme => scheme with { PassedLineBgColor = value });
        }
    }

    [DisplayName("Цвет текста пройденной строки таблицы")]
    public Color PassedLineTextColor
    {
        get => _passedLineTextColor ??= ColorScheme.Default.PassedLineTextColor;
        set
        {
            if (_passedLineTextColor == value) return;
            _passedLineTextColor = value;
            RegisterColorMutation(scheme => scheme with { PassedLineTextColor = value });
        }
    }

    [DisplayName("Цвет фона заблокированной ячейки")]
    public Color BlockedBgColor
    {
        get => _blockedBgColor ??= ColorScheme.Default.BlockedBgColor;
        set
        {
            if (_blockedBgColor == value) return;
            _blockedBgColor = value;
            RegisterColorMutation(scheme => scheme with { BlockedBgColor = value });
        }
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
        if (_pendingColorMutations.Count == 0) return;
        if (_colorSchemeProvider == null) return;

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