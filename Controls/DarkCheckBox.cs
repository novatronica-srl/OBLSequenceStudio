using System.Windows.Forms.VisualStyles;

namespace OBLSequenceStudio.Controls;

internal sealed class DarkCheckBox : CheckBox
{
    public DarkCheckBox() => ForeColor = Color.WhiteSmoke;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Enabled) return;

        // Keep the native disabled checkbox glyph, but replace its low-contrast text.
        int glyphWidth = CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.UncheckedDisabled).Width;
        var textBounds = new Rectangle(Padding.Left + glyphWidth + 3, Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Horizontal - glyphWidth - 3),
            Math.Max(0, ClientSize.Height - Padding.Vertical));
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, textBounds);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, Color.FromArgb(190, 202, 218),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}
