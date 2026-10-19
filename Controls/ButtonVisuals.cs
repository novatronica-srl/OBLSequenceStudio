using System.Drawing.Drawing2D;

namespace OBLSequenceStudio.Controls;

/// <summary>Applies shared button interaction styling and draws action icons without external image assets.</summary>
internal static class ButtonVisuals
{
    /// <summary>Configures hover and pressed visuals and the initial icon for an action button.</summary>
    public static void Configure(Button button, string icon)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = Color.FromArgb(39, 52, 72);
        button.ForeColor = Color.WhiteSmoke;
        button.FlatAppearance.BorderColor = Color.FromArgb(94, 119, 149);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(54, 78, 106);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(32, 95, 132);
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(6, 2, 6, 2);
        button.MinimumSize = new Size(0, 30);
        SetIcon(button, icon);
        button.DpiChangedAfterParent += (_, _) => SetIcon(button, button.Image?.Tag as string ?? icon);
        button.Paint += PaintDisabled;
        button.EnabledChanged += (_, _) => button.Invalidate();
        button.Disposed += (_, _) => button.Image?.Dispose();
    }

    private static void PaintDisabled(object? sender, PaintEventArgs e)
    {
        if (sender is not Button button || button.Enabled) return;
        // WinForms ignores ForeColor for disabled buttons and uses the system
        // disabled text color, which has insufficient contrast on our dark theme.
        var bounds = button.ClientRectangle;
        if (bounds.Width < 2 || bounds.Height < 2) return;
        using var background = new SolidBrush(button.BackColor);
        e.Graphics.FillRectangle(background, bounds);
        using var border = new Pen(Color.FromArgb(75, 88, 106));
        e.Graphics.DrawRectangle(border, 0, 0, bounds.Width - 1, bounds.Height - 1);

        Color textColor = button.BackColor.GetBrightness() > 0.5f
            ? Color.FromArgb(60, 65, 70) : Color.FromArgb(190, 202, 218);
        var content = Rectangle.FromLTRB(button.Padding.Left + 2, button.Padding.Top,
            bounds.Width - button.Padding.Right - 2, bounds.Height - button.Padding.Bottom);
        int imageWidth = button.Image?.Width ?? 0;
        int gap = imageWidth > 0 && button.Text.Length > 0 ? button.LogicalToDeviceUnits(6) : 0;
        int textWidth = TextRenderer.MeasureText(button.Text, button.Font).Width;
        int groupWidth = Math.Min(content.Width, imageWidth + gap + textWidth);
        int left = content.Left + Math.Max(0, (content.Width - groupWidth) / 2);
        if (button.Image is { } image)
            e.Graphics.DrawImage(image, left, content.Top + (content.Height - image.Height) / 2, image.Width, image.Height);
        var textBounds = new Rectangle(left + imageWidth + gap, content.Top,
            Math.Max(0, groupWidth - imageWidth - gap), content.Height);
        TextRenderer.DrawText(e.Graphics, button.Text, button.Font, textBounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    /// <summary>Replaces a button action icon, disposing the previous image when it is replaced.</summary>
    public static void SetIcon(Button button, string key)
    {
        int size = button.LogicalToDeviceUnits(20);
        var bitmap = new Bitmap(size, size) { Tag = key };
        using (var g = Graphics.FromImage(bitmap))
        {
            g.ScaleTransform(size / 20f, size / 20f);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(button.ForeColor, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var fill = new SolidBrush(button.ForeColor);
            void Line(float x, float y, float x2, float y2) => g.DrawLine(pen, x, y, x2, y2);
            void Box(float x, float y, float width, float height) => g.DrawRectangle(pen, x, y, width, height);
            switch (key)
            {
                case "StartBoard": case "ResumeBoard":
                    g.FillPolygon(fill, new[] { new Point(6, 3), new Point(17, 10), new Point(6, 17) }); break;
                case "PauseBoard": g.FillRectangle(fill, 5, 3, 4, 14); g.FillRectangle(fill, 12, 3, 4, 14); break;
                case "StopBoard": g.FillRectangle(fill, 4, 4, 12, 12); break;
                case "RestartBoard": g.DrawArc(pen, 3, 3, 14, 14, -90, 285); Line(3, 3, 3, 9); Line(3, 9, 9, 9); break;
                case "FirmwareUpdate":
                    Box(5, 5, 10, 10); for (int n = 6; n <= 14; n += 4) { Line(n, 2, n, 5); Line(n, 15, n, 18); Line(2, n, 5, n); Line(15, n, 18, n); } break;
                case "SaveBoard": Box(3, 2, 14, 16); Box(6, 2, 8, 5); Box(6, 11, 8, 7); break;
                case "Connect": case "Disconnect":
                    Box(5, 7, 10, 7); Line(7, 3, 7, 7); Line(13, 3, 13, 7); Line(10, 14, 10, 18);
                    if (key == "Disconnect") Line(2, 18, 18, 2);
                    break;
                case "Play": g.FillPolygon(fill, new[] { new Point(6, 3), new Point(17, 10), new Point(6, 17) }); break;
                case "Pause": g.FillRectangle(fill, 5, 3, 4, 14); g.FillRectangle(fill, 12, 3, 4, 14); break;
                case "Stop": g.FillRectangle(fill, 4, 4, 12, 12); break;
                case "New": Box(4, 2, 12, 16); Line(7, 10, 13, 10); Line(10, 7, 10, 13); break;
                case "Open":
                    g.DrawPolygon(pen, new[] { new Point(2, 5), new Point(8, 5), new Point(10, 7), new Point(18, 7), new Point(16, 17), new Point(2, 17) }); break;
                case "Save": case "SaveAs":
                    Box(3, 2, 14, 16); Box(6, 2, 8, 5); Box(6, 11, 8, 7);
                    if (key == "SaveAs") { Line(12, 17, 18, 11); Line(16, 11, 18, 13); }
                    break;
                case "Export": case "ImportProgrammer":
                    Box(3, 3, 9, 14); Line(8, 10, 18, 10);
                    if (key == "Export") { Line(15, 7, 18, 10); Line(15, 13, 18, 10); }
                    else { Line(11, 7, 8, 10); Line(11, 13, 8, 10); }
                    break;
                case "AddStep": Line(3, 10, 17, 10); Line(10, 3, 10, 17); break;
                case "DeleteStep": case "Clear":
                    Line(3, 5, 17, 5); Box(7, 2, 6, 3); Box(5, 5, 10, 13); Line(8, 8, 8, 15); Line(12, 8, 12, 15); break;
                case "Copy": Box(3, 2, 10, 13); Box(7, 6, 10, 12); break;
                case "SingleStep": Box(3, 4, 14, 12); Line(10, 7, 10, 13); break;
                case "Color":
                    g.DrawEllipse(pen, 2, 2, 16, 16);
                    using (var red = new SolidBrush(Color.FromArgb(255, 110, 115))) g.FillEllipse(red, 5, 5, 4, 4);
                    using (var green = new SolidBrush(Color.FromArgb(110, 230, 145))) g.FillEllipse(green, 11, 5, 4, 4);
                    using (var blue = new SolidBrush(Color.FromArgb(100, 185, 255))) g.FillEllipse(blue, 8, 11, 4, 4);
                    break;
                case "No": Line(5, 5, 15, 15); Line(15, 5, 5, 15); break;
                default: Line(3, 10, 8, 15); Line(8, 15, 17, 5); break;
            }
        }
        var previous = button.Image;
        button.Image = bitmap;
        previous?.Dispose();
    }
}
