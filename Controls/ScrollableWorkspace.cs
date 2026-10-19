namespace OBLSequenceStudio.Controls;

/// <summary>Preserves access to the full workspace when display scaling leaves too little screen space.</summary>
internal static class ScrollableWorkspace
{
    /// <summary>Hosts a layout in a scrollable viewport with a minimum size expressed at 96 DPI.</summary>
    public static void Attach(Form form, Control content, Size logicalContentSize)
    {
        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = Padding.Empty,
            BackColor = form.BackColor
        };
        form.Controls.Remove(content);
        content.Dock = DockStyle.None;
        content.Location = Point.Empty;
        viewport.Controls.Add(content);
        form.Controls.Add(viewport);
        form.MinimumSize = new Size(640, 400);

        void FitContent()
        {
            Size minimum = form.LogicalToDeviceUnits(logicalContentSize);
            // Start from the full viewport so obsolete scrollbars do not keep each other visible.
            int scrollWidth = SystemInformation.GetVerticalScrollBarWidthForDpi(form.DeviceDpi);
            int scrollHeight = SystemInformation.GetHorizontalScrollBarHeightForDpi(form.DeviceDpi);
            bool horizontal = minimum.Width > viewport.Width;
            bool vertical = minimum.Height > viewport.Height;
            for (int pass = 0; pass < 2; pass++)
            {
                horizontal = minimum.Width > viewport.Width - (vertical ? scrollWidth : 0);
                vertical = minimum.Height > viewport.Height - (horizontal ? scrollHeight : 0);
            }
            var available = new Size(viewport.Width - (vertical ? scrollWidth : 0),
                viewport.Height - (horizontal ? scrollHeight : 0));
            content.Size = new Size(Math.Max(minimum.Width, available.Width),
                Math.Max(minimum.Height, available.Height));
        }

        viewport.Layout += (_, _) => FitContent();
        form.DpiChanged += (_, _) => form.BeginInvoke((Action)FitContent);
        FitContent();
    }
}