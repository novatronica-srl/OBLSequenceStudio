using System.Drawing;
using System.Windows.Forms;

namespace OBLSequenceStudio.Controls;

partial class FirmwareUpdateWizard
{
    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FirmwareUpdateWizard));
        _heading = new Label();
        _instructions = new TextBox();
        _file = new TextBox();
        _ports = new ComboBox();
        _prepared = new CheckBox();
        _choices = new FlowLayoutPanel();
        _browse = new Button();
        _refreshPorts = new Button();
        _progress = new ProgressBar();
        _back = new Button();
        _next = new Button();
        _cancel = new Button();
        wizardLayout = new TableLayoutPanel();
        navigationButtons = new FlowLayoutPanel();
        _choices.SuspendLayout();
        wizardLayout.SuspendLayout();
        navigationButtons.SuspendLayout();
        SuspendLayout();
        // 
        // _heading
        // 
        _heading.AutoSize = true;
        _heading.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        _heading.Location = new Point(23, 20);
        _heading.Name = "_heading";
        _heading.Size = new Size(209, 25);
        _heading.TabIndex = 0;
        _heading.Text = "1. Seleziona il firmware";
        // 
        // _instructions
        // 
        _instructions.BackColor = Color.FromArgb(27, 34, 48);
        _instructions.BorderStyle = BorderStyle.None;
        _instructions.Dock = DockStyle.Fill;
        _instructions.ForeColor = Color.WhiteSmoke;
        _instructions.Location = new Point(23, 68);
        _instructions.Multiline = true;
        _instructions.Name = "_instructions";
        _instructions.ReadOnly = true;
        _instructions.ScrollBars = ScrollBars.Vertical;
        _instructions.Size = new Size(758, 240);
        _instructions.TabIndex = 1;
        _instructions.TabStop = false;
        _instructions.Text = resources.GetString("_instructions.Text");
        // 
        // _file
        // 
        _file.BackColor = SystemColors.Window;
        _file.ForeColor = SystemColors.WindowText;
        _file.Location = new Point(3, 3);
        _file.Name = "_file";
        _file.ReadOnly = true;
        _file.Size = new Size(730, 25);
        _file.TabIndex = 0;
        // 
        // _ports
        // 
        _ports.BackColor = SystemColors.Window;
        _ports.DropDownStyle = ComboBoxStyle.DropDownList;
        _ports.ForeColor = SystemColors.WindowText;
        _ports.Location = new Point(3, 69);
        _ports.Name = "_ports";
        _ports.Size = new Size(150, 25);
        _ports.TabIndex = 2;
        // 
        // _prepared
        // 
        _prepared.AutoSize = true;
        _prepared.Location = new Point(3, 133);
        _prepared.Name = "_prepared";
        _prepared.Size = new Size(303, 23);
        _prepared.TabIndex = 4;
        _prepared.Text = "Ho eseguito i passaggi e la scheda è in BOOT";
        // 
        // _choices
        // 
        _choices.Controls.Add(_file);
        _choices.Controls.Add(_browse);
        _choices.Controls.Add(_ports);
        _choices.Controls.Add(_refreshPorts);
        _choices.Controls.Add(_prepared);
        _choices.Dock = DockStyle.Fill;
        _choices.FlowDirection = FlowDirection.TopDown;
        _choices.Location = new Point(23, 314);
        _choices.Name = "_choices";
        _choices.Size = new Size(758, 164);
        _choices.TabIndex = 2;
        _choices.WrapContents = false;
        // 
        // _browse
        // 
        _browse.AutoSize = true;
        _browse.Location = new Point(3, 34);
        _browse.Name = "_browse";
        _browse.Size = new Size(110, 29);
        _browse.TabIndex = 1;
        _browse.Text = "Scegli file .bin…";
        // 
        // _refreshPorts
        // 
        _refreshPorts.AutoSize = true;
        _refreshPorts.Location = new Point(3, 98);
        _refreshPorts.Name = "_refreshPorts";
        _refreshPorts.Size = new Size(149, 29);
        _refreshPorts.TabIndex = 3;
        _refreshPorts.Text = "Aggiorna porte COM";
        // 
        // _progress
        // 
        _progress.Dock = DockStyle.Fill;
        _progress.Location = new Point(23, 484);
        _progress.Name = "_progress";
        _progress.Size = new Size(758, 19);
        _progress.TabIndex = 3;
        // 
        // _back
        // 
        _back.AutoSize = true;
        _back.Location = new Point(518, 13);
        _back.Name = "_back";
        _back.Size = new Size(75, 29);
        _back.TabIndex = 2;
        _back.Text = "Indietro";
        // 
        // _next
        // 
        _next.AutoSize = true;
        _next.Location = new Point(599, 13);
        _next.Name = "_next";
        _next.Size = new Size(75, 29);
        _next.TabIndex = 1;
        _next.Text = "Avanti";
        // 
        // _cancel
        // 
        _cancel.AutoSize = true;
        _cancel.Location = new Point(680, 13);
        _cancel.Name = "_cancel";
        _cancel.Size = new Size(75, 29);
        _cancel.TabIndex = 0;
        _cancel.Text = "Annulla";
        // 
        // wizardLayout
        // 
        wizardLayout.ColumnCount = 1;
        wizardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        wizardLayout.Controls.Add(_heading, 0, 0);
        wizardLayout.Controls.Add(_instructions, 0, 1);
        wizardLayout.Controls.Add(_choices, 0, 2);
        wizardLayout.Controls.Add(_progress, 0, 3);
        wizardLayout.Controls.Add(navigationButtons, 0, 4);
        wizardLayout.Dock = DockStyle.Fill;
        wizardLayout.Location = new Point(0, 0);
        wizardLayout.Name = "wizardLayout";
        wizardLayout.Padding = new Padding(20);
        wizardLayout.RowCount = 5;
        wizardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        wizardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        wizardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
        wizardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
        wizardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
        wizardLayout.Size = new Size(804, 581);
        wizardLayout.TabIndex = 0;
        // 
        // navigationButtons
        // 
        navigationButtons.Controls.Add(_cancel);
        navigationButtons.Controls.Add(_next);
        navigationButtons.Controls.Add(_back);
        navigationButtons.Dock = DockStyle.Fill;
        navigationButtons.FlowDirection = FlowDirection.RightToLeft;
        navigationButtons.Location = new Point(23, 509);
        navigationButtons.Name = "navigationButtons";
        navigationButtons.Padding = new Padding(0, 10, 0, 0);
        navigationButtons.Size = new Size(758, 49);
        navigationButtons.TabIndex = 4;
        navigationButtons.WrapContents = false;
        // 
        // FirmwareUpdateWizard
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(27, 34, 48);
        ClientSize = new Size(804, 581);
        Controls.Add(wizardLayout);
        Font = new Font("Segoe UI", 10F);
        ForeColor = Color.WhiteSmoke;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(760, 560);
        Name = "FirmwareUpdateWizard";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Aggiorna firmware…";
        _choices.ResumeLayout(false);
        _choices.PerformLayout();
        wizardLayout.ResumeLayout(false);
        wizardLayout.PerformLayout();
        navigationButtons.ResumeLayout(false);
        navigationButtons.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Label _heading = null!;
    private TextBox _instructions = null!;
    private TextBox _file = null!;
    private ComboBox _ports = null!;
    private CheckBox _prepared = null!;
    private FlowLayoutPanel _choices = null!;
    private ProgressBar _progress = null!;
    private Button _back = null!;
    private Button _next = null!;
    private Button _cancel = null!;
    private Button _browse = null!;
    private Button _refreshPorts = null!;
    private TableLayoutPanel wizardLayout = null!;
    private FlowLayoutPanel navigationButtons = null!;
}
