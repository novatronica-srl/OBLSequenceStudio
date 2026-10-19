using System.Windows.Forms.VisualStyles;

namespace OBLSequenceStudio.Controls;

/// <summary>Keeps disabled checkbox text readable on dark backgrounds while retaining the native checkbox glyph.</summary>
internal sealed class DarkCheckBox : CheckBox
{
    /// <summary>Sets the default foreground for the dark preview panel.</summary>
    public DarkCheckBox() => ForeColor = Color.WhiteSmoke;

    /// <summary>Repaints disabled text with greater contrast after the native checkbox has been drawn.</summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Enabled) return;

        // Keep the native disabled checkbox glyph, but replace its low-contrast text.
        int glyphWidth = CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.UncheckedDisabled).Width;
        var textBounds = new Rectangle(Padding.Left + glyphWidth + LogicalToDeviceUnits(3), Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Horizontal - glyphWidth - LogicalToDeviceUnits(3)),
            Math.Max(0, ClientSize.Height - Padding.Vertical));
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, textBounds);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, Color.FromArgb(190, 202, 218),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
