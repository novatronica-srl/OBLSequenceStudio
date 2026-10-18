using OBLSequenceStudio.Controls;

namespace OBLSequenceStudio.Localization;

public static class Dialogs
{
    // Native MessageBox buttons follow Windows' language, not the application's selection.
    public static DialogResult Show(string message, string caption = "OBL Sequence Studio",
        MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        using var hints = new UiHints();
        using var dialog = new Form
        {
            Text = caption, FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false,
            ShowInTaskbar = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Font = SystemFonts.MessageBoxFont, Padding = new Padding(16)
        };
        DarkTitleBar.Attach(dialog);
        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2, Dock = DockStyle.Fill };
        var messageLabel = new Label
        {
            Text = message, AutoSize = true, MaximumSize = new Size(560, 0), MinimumSize = new Size(300, 0),
            Margin = new Padding(0, 0, 0, 20), AccessibleName = caption
        };
        hints.Set(messageLabel, "DialogMessage");
        layout.Controls.Add(messageLabel);
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        Button Add(string key, DialogResult result)
        {
            var button = new Button { Text = Localizer.Text(key), DialogResult = result, AutoSize = true, MinimumSize = new Size(85, 32) };
            ButtonVisuals.Configure(button, key);
            hints.Set(button, key);
            actions.Controls.Add(button);
            return button;
        }
        if (buttons == MessageBoxButtons.YesNo)
        {
            var no = Add("No", DialogResult.No);
            Add("Yes", DialogResult.Yes);
            dialog.AcceptButton = no;
            dialog.CancelButton = no;
        }
        else
        {
            var ok = Add("OK", DialogResult.OK);
            dialog.AcceptButton = ok;
            dialog.CancelButton = ok;
        }
        layout.Controls.Add(actions);
        dialog.Controls.Add(layout);
        dialog.AccessibleDescription = icon.ToString();
        return dialog.ShowDialog(Form.ActiveForm);
    }
}
