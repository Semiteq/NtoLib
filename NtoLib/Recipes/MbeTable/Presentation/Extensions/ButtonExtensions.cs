using System;
using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Presentation.Extensions;

/// <summary>
/// Extension methods for Button styling and behavior configuration.
/// </summary>
public static class ButtonExtensions
{
    /// <summary>
    /// Configures a button to display custom colors when disabled.
    /// Subscribes to EnabledChanged event and applies colors automatically.
    /// </summary>
    /// <param name="button">The button to configure.</param>
    /// <param name="disabledBackColor">Background color to apply when button is disabled.</param>
    /// <param name="disabledForeColor">Foreground color to apply when button is disabled.</param>
    public static void SetupDisabledStyle(this Button button, Color disabledBackColor, Color disabledForeColor)
    {
        if (button == null)
            throw new ArgumentNullException(nameof(button));

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;

        var normalBackColor = button.BackColor;
        var normalForeColor = button.ForeColor;

        button.EnabledChanged += (sender, e) =>
        {
            if (button.IsDisposed)
                return;

            if (button.Enabled)
            {
                button.BackColor = normalBackColor;
                button.ForeColor = normalForeColor;
            }
            else
            {
                button.BackColor = disabledBackColor;
                button.ForeColor = disabledForeColor;
            }
        };
    }
}