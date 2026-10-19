using OBLSequenceStudio.Controls;

namespace OBLSequenceStudio;

partial class MainForm
{
    #region Windows Form Designer generated code

    /// <summary>Required method for Designer support. Edit the layout with the visual designer.</summary>
    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        _timeline = new TimelineControl();
        _grid = new DataGridView();
        stepColumn = new DataGridViewTextBoxColumn();
        startColumn = new DataGridViewTextBoxColumn();
        endColumn = new DataGridViewTextBoxColumn();
        durationColumn = new DataGridViewTextBoxColumn();
        positionColumn = new DataGridViewTextBoxColumn();
        modeColumn = new DataGridViewTextBoxColumn();
        parametersColumn = new DataGridViewTextBoxColumn();
        _channels = new ListBox();
        _summary = new Label();
        _status = new Label();
        _cursor = new Label();
        _destination = new NumericUpDown();
        _language = new ComboBox();
        _stepEditor = new StepEditorControl();
        _repeat = new Label();
        _ledPreview = new LedPreviewControl();
        _connect = new Button();
        _live = new CheckBox();
        _hardwareTarget = new ComboBox();
        _boardState = new Label();
        _newButton = new Button();
        _openButton = new Button();
        _saveButton = new Button();
        _saveAsButton = new Button();
        _firmwareButton = new Button();
        _addStepButton = new Button();
        _deleteStepButton = new Button();
        _clearButton = new Button();
        _copyButton = new Button();
        _singleStepButton = new Button();
        _applyBoardButton = new Button();
        _startBoardButton = new Button();
        _pauseBoardButton = new Button();
        _resumeBoardButton = new Button();
        _stopBoardButton = new Button();
        _restartBoardButton = new Button();
        _saveBoardButton = new Button();
        _languageLabel = new Label();
        _channelsLabel = new Label();
        outer = new TableLayoutPanel();
        bars = new TableLayoutPanel();
        header = new TableLayoutPanel();
        toolbar = new FlowLayoutPanel();
        languagePanel = new FlowLayoutPanel();
        hardware = new FlowLayoutPanel();
        vertical = new SplitContainer();
        previewLayout = new TableLayoutPanel();
        scroll = new Panel();
        bottom = new SplitContainer();
        chPanel = new Panel();
        detail = new Panel();
        rightTop = new FlowLayoutPanel();
        statusBar = new Panel();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_destination).BeginInit();
        outer.SuspendLayout();
        bars.SuspendLayout();
        header.SuspendLayout();
        toolbar.SuspendLayout();
        languagePanel.SuspendLayout();
        hardware.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)vertical).BeginInit();
        vertical.Panel1.SuspendLayout();
        vertical.Panel2.SuspendLayout();
        vertical.SuspendLayout();
        previewLayout.SuspendLayout();
        scroll.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)bottom).BeginInit();
        bottom.Panel1.SuspendLayout();
        bottom.Panel2.SuspendLayout();
        bottom.SuspendLayout();
        chPanel.SuspendLayout();
        detail.SuspendLayout();
        rightTop.SuspendLayout();
        statusBar.SuspendLayout();
        SuspendLayout();
        // 
        // _timeline
        // 
        _timeline.BackColor = Color.FromArgb(18, 24, 37);
        _timeline.Dock = DockStyle.Top;
        _timeline.ForeColor = Color.FromArgb(217, 226, 240);
        _timeline.Location = new Point(0, 0);
        _timeline.Name = "_timeline";
        _timeline.Size = new Size(1067, 1048);
        _timeline.TabIndex = 0;
        // 
        // _grid
        // 
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.FromArgb(27, 34, 48);
        _grid.BorderStyle = BorderStyle.None;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(40, 53, 73);

        dataGridViewCellStyle1.ForeColor = Color.White;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        _grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.Columns.AddRange(new DataGridViewColumn[] { stepColumn, startColumn, endColumn, durationColumn, positionColumn, modeColumn, parametersColumn });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(29, 39, 56);

        dataGridViewCellStyle2.ForeColor = Color.WhiteSmoke;
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(24, 110, 153);
        dataGridViewCellStyle2.SelectionForeColor = Color.White;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        _grid.DefaultCellStyle = dataGridViewCellStyle2;
        _grid.Dock = DockStyle.Fill;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        _grid.EnableHeadersVisualStyles = false;
        _grid.GridColor = Color.FromArgb(61, 75, 92);
        _grid.Location = new Point(8, 96);
        _grid.MultiSelect = false;
        _grid.Name = "_grid";
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 33;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Size = new Size(861, 174);
        _grid.TabIndex = 0;
        // 
        // stepColumn
        // 
        startColumn.Name = "startColumn";
        startColumn.DataPropertyName = "Start";
        endColumn.Name = "endColumn";
        endColumn.DataPropertyName = "End";
        durationColumn.Name = "durationColumn";
        durationColumn.DataPropertyName = "Duration";
        stepColumn.FillWeight = 55F;
        stepColumn.HeaderText = "STEP";
        stepColumn.Name = "stepColumn";
        stepColumn.ReadOnly = true;
        // 
        // positionColumn
        // 
        positionColumn.HeaderText = "POSIZIONE FINALE (ms)";
        positionColumn.Name = "positionColumn";
        // 
        // modeColumn
        // 
        modeColumn.HeaderText = "Modalità";
        modeColumn.Name = "modeColumn";
        modeColumn.ReadOnly = true;
        // 
        // parametersColumn
        // 
        parametersColumn.FillWeight = 180F;
        parametersColumn.HeaderText = "Parametri";
        parametersColumn.Name = "parametersColumn";
        parametersColumn.ReadOnly = true;
        // 
        // _channels
        // 
        _channels.BackColor = Color.FromArgb(27, 34, 48);
        _channels.BorderStyle = BorderStyle.None;
        _channels.Dock = DockStyle.Fill;
        _channels.ForeColor = Color.WhiteSmoke;
        _channels.IntegralHeight = false;
        _channels.Location = new Point(5, 41);
        _channels.Name = "_channels";
        _channels.Size = new Size(190, 232);
        _channels.TabIndex = 0;
        // 
        // _summary
        // 
        _summary.AutoSize = true;
        rightTop.SetFlowBreak(_summary, true);
        _summary.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _summary.Location = new Point(5, 12);
        _summary.Margin = new Padding(5, 12, 25, 0);
        _summary.Name = "_summary";
        _summary.Size = new Size(0, 21);
        _summary.TabIndex = 0;
        // 
        // _status
        // 
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(159, 178, 199);
        _status.Location = new Point(0, 0);
        _status.Name = "_status";
        _status.Size = new Size(1194, 24);
        _status.TabIndex = 0;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _cursor
        // 
        _cursor.Dock = DockStyle.Right;
        _cursor.ForeColor = Color.FromArgb(255, 194, 94);
        _cursor.Location = new Point(1194, 0);
        _cursor.Name = "_cursor";
        _cursor.Size = new Size(240, 24);
        _cursor.TabIndex = 1;
        _cursor.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _destination
        // 
        _destination.Location = new Point(660, 51);
        _destination.Margin = new Padding(3, 8, 3, 0);
        _destination.Maximum = new decimal(new int[] { 28, 0, 0, 0 });
        _destination.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _destination.Name = "_destination";
        _destination.Size = new Size(65, 24);
        _destination.TabIndex = 7;
        _destination.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // _language
        // 
        _language.BackColor = SystemColors.Window;
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.ForeColor = SystemColors.WindowText;
        _language.Location = new Point(64, 8);
        _language.Margin = new Padding(3, 8, 0, 0);
        _language.Name = "_language";
        _language.Size = new Size(115, 25);
        _language.TabIndex = 1;
        // 
        // _stepEditor
        // 
        _stepEditor.AutoScroll = true;
        _stepEditor.Dock = DockStyle.Right;
        _stepEditor.Location = new Point(869, 96);
        _stepEditor.Name = "_stepEditor";
        _stepEditor.Size = new Size(350, 174);
        _stepEditor.TabIndex = 1;
        // 
        // _repeat
        // 
        _repeat.AutoSize = true;
        _repeat.Location = new Point(499, 55);
        _repeat.Margin = new Padding(8, 12, 3, 0);
        _repeat.Name = "_repeat";
        _repeat.Size = new Size(155, 17);
        _repeat.TabIndex = 6;
        _repeat.Text = "Sequenza ciclica continua";
        // 
        // _ledPreview
        // 
        _ledPreview.Dock = DockStyle.Fill;
        _ledPreview.Location = new Point(1087, 3);
        _ledPreview.Name = "_ledPreview";
        _ledPreview.Size = new Size(344, 451);
        _ledPreview.TabIndex = 0;
        // 
        // _connect
        // 
        _connect.AutoSize = true;
        _connect.Location = new Point(401, 4);
        _connect.Margin = new Padding(4);
        _connect.Name = "_connect";
        _connect.Size = new Size(111, 27);
        _connect.TabIndex = 4;
        _connect.Text = "Connetti scheda";
        // 
        // _live
        // 
        _live.AutoSize = true;
        _live.Checked = true;
        _live.CheckState = CheckState.Checked;
        _live.ForeColor = Color.WhiteSmoke;
        _live.Location = new Point(521, 10);
        _live.Margin = new Padding(5, 10, 8, 0);
        _live.Name = "_live";
        _live.Size = new Size(108, 21);
        _live.TabIndex = 5;
        _live.Text = "Modifiche live";
        // 
        // _hardwareTarget
        // 
        _hardwareTarget.BackColor = SystemColors.Window;
        _hardwareTarget.DropDownStyle = ComboBoxStyle.DropDownList;
        _hardwareTarget.ForeColor = SystemColors.WindowText;
        _hardwareTarget.Location = new Point(4, 8);
        _hardwareTarget.Margin = new Padding(4, 8, 4, 0);
        _hardwareTarget.Name = "_hardwareTarget";
        _hardwareTarget.Size = new Size(145, 25);
        _hardwareTarget.TabIndex = 0;
        // 
        // _boardState
        // 
        _boardState.AutoSize = true;
        _boardState.Location = new Point(5, 90);
        _boardState.Margin = new Padding(5, 2, 0, 2);
        _boardState.Name = "_boardState";
        _boardState.Size = new Size(0, 17);
        _boardState.TabIndex = 2;
        _boardState.Visible = false;
        // 
        // _newButton
        // 
        _newButton.AutoSize = true;
        _newButton.BackColor = Color.FromArgb(27, 34, 48);
        _newButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _newButton.FlatStyle = FlatStyle.Flat;
        _newButton.ForeColor = Color.WhiteSmoke;
        _newButton.Location = new Point(4, 4);
        _newButton.Margin = new Padding(4);
        _newButton.Name = "_newButton";
        _newButton.Padding = new Padding(8, 3, 8, 3);
        _newButton.Size = new Size(75, 35);
        _newButton.TabIndex = 0;
        _newButton.Text = "Nuovo";
        _newButton.UseVisualStyleBackColor = false;
        // 
        // _openButton
        // 
        _openButton.AutoSize = true;
        _openButton.BackColor = Color.FromArgb(27, 34, 48);
        _openButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _openButton.FlatStyle = FlatStyle.Flat;
        _openButton.ForeColor = Color.WhiteSmoke;
        _openButton.Location = new Point(87, 4);
        _openButton.Margin = new Padding(4);
        _openButton.Name = "_openButton";
        _openButton.Padding = new Padding(8, 3, 8, 3);
        _openButton.Size = new Size(75, 35);
        _openButton.TabIndex = 1;
        _openButton.Text = "Apri";
        _openButton.UseVisualStyleBackColor = false;
        // 
        // _saveButton
        // 
        _saveButton.AutoSize = true;
        _saveButton.BackColor = Color.FromArgb(27, 34, 48);
        _saveButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _saveButton.FlatStyle = FlatStyle.Flat;
        _saveButton.ForeColor = Color.WhiteSmoke;
        _saveButton.Location = new Point(170, 4);
        _saveButton.Margin = new Padding(4);
        _saveButton.Name = "_saveButton";
        _saveButton.Padding = new Padding(8, 3, 8, 3);
        _saveButton.Size = new Size(75, 35);
        _saveButton.TabIndex = 2;
        _saveButton.Text = "Salva";
        _saveButton.UseVisualStyleBackColor = false;
        // 
        // _saveAsButton
        // 
        _saveAsButton.AutoSize = true;
        _saveAsButton.BackColor = Color.FromArgb(27, 34, 48);
        _saveAsButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _saveAsButton.FlatStyle = FlatStyle.Flat;
        _saveAsButton.ForeColor = Color.WhiteSmoke;
        _saveAsButton.Location = new Point(253, 4);
        _saveAsButton.Margin = new Padding(4, 4, 16, 4);
        _saveAsButton.Name = "_saveAsButton";
        _saveAsButton.Padding = new Padding(8, 3, 8, 3);
        _saveAsButton.Size = new Size(128, 35);
        _saveAsButton.TabIndex = 3;
        _saveAsButton.Text = "Salva con nome";
        _saveAsButton.UseVisualStyleBackColor = false;
        // 
        // _firmwareButton
        // 
        _firmwareButton.AutoSize = true;
        _firmwareButton.BackColor = Color.FromArgb(27, 34, 48);
        _firmwareButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _firmwareButton.FlatStyle = FlatStyle.Flat;
        _firmwareButton.ForeColor = Color.WhiteSmoke;
        _firmwareButton.Location = new Point(641, 4);
        _firmwareButton.Margin = new Padding(4);
        _firmwareButton.Name = "_firmwareButton";
        _firmwareButton.Padding = new Padding(8, 3, 8, 3);
        _firmwareButton.Size = new Size(154, 35);
        _firmwareButton.TabIndex = 6;
        _firmwareButton.Text = "Aggiorna firmware…";
        _firmwareButton.UseVisualStyleBackColor = false;
        // 
        // _addStepButton
        // 
        _addStepButton.AutoSize = true;
        _addStepButton.BackColor = Color.FromArgb(27, 34, 48);
        _addStepButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _addStepButton.FlatStyle = FlatStyle.Flat;
        _addStepButton.ForeColor = Color.WhiteSmoke;
        _addStepButton.Location = new Point(4, 47);
        _addStepButton.Margin = new Padding(4);
        _addStepButton.Name = "_addStepButton";
        _addStepButton.Padding = new Padding(8, 3, 8, 3);
        _addStepButton.Size = new Size(75, 35);
        _addStepButton.TabIndex = 1;
        _addStepButton.Text = "+ Step";
        _addStepButton.UseVisualStyleBackColor = false;
        // 
        // _deleteStepButton
        // 
        _deleteStepButton.AutoSize = true;
        _deleteStepButton.BackColor = Color.FromArgb(27, 34, 48);
        _deleteStepButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _deleteStepButton.FlatStyle = FlatStyle.Flat;
        _deleteStepButton.ForeColor = Color.WhiteSmoke;
        _deleteStepButton.Location = new Point(87, 47);
        _deleteStepButton.Margin = new Padding(4);
        _deleteStepButton.Name = "_deleteStepButton";
        _deleteStepButton.Padding = new Padding(8, 3, 8, 3);
        _deleteStepButton.Size = new Size(106, 35);
        _deleteStepButton.TabIndex = 2;
        _deleteStepButton.Text = "Elimina step";
        _deleteStepButton.UseVisualStyleBackColor = false;
        // 
        // _clearButton
        // 
        _clearButton.AutoSize = true;
        _clearButton.BackColor = Color.FromArgb(27, 34, 48);
        _clearButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _clearButton.FlatStyle = FlatStyle.Flat;
        _clearButton.ForeColor = Color.WhiteSmoke;
        _clearButton.Location = new Point(201, 47);
        _clearButton.Margin = new Padding(4);
        _clearButton.Name = "_clearButton";
        _clearButton.Padding = new Padding(8, 3, 8, 3);
        _clearButton.Size = new Size(75, 35);
        _clearButton.TabIndex = 3;
        _clearButton.Text = "Svuota";
        _clearButton.UseVisualStyleBackColor = false;
        // 
        // _copyButton
        // 
        _copyButton.AutoSize = true;
        _copyButton.BackColor = Color.FromArgb(27, 34, 48);
        _copyButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _copyButton.FlatStyle = FlatStyle.Flat;
        _copyButton.ForeColor = Color.WhiteSmoke;
        _copyButton.Location = new Point(284, 47);
        _copyButton.Margin = new Padding(4);
        _copyButton.Name = "_copyButton";
        _copyButton.Padding = new Padding(8, 3, 8, 3);
        _copyButton.Size = new Size(85, 35);
        _copyButton.TabIndex = 4;
        _copyButton.Text = "Copia →";
        _copyButton.UseVisualStyleBackColor = false;
        // 
        // _singleStepButton
        // 
        _singleStepButton.AutoSize = true;
        _singleStepButton.BackColor = Color.FromArgb(27, 34, 48);
        _singleStepButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _singleStepButton.FlatStyle = FlatStyle.Flat;
        _singleStepButton.ForeColor = Color.WhiteSmoke;
        _singleStepButton.Location = new Point(377, 47);
        _singleStepButton.Margin = new Padding(4);
        _singleStepButton.Name = "_singleStepButton";
        _singleStepButton.Padding = new Padding(8, 3, 8, 3);
        _singleStepButton.Size = new Size(110, 35);
        _singleStepButton.TabIndex = 5;
        _singleStepButton.Text = "Un solo step";
        _singleStepButton.UseVisualStyleBackColor = false;
        // 
        // _applyBoardButton
        // 
        _applyBoardButton.AutoSize = true;
        _applyBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _applyBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _applyBoardButton.FlatStyle = FlatStyle.Flat;
        _applyBoardButton.ForeColor = Color.WhiteSmoke;
        _applyBoardButton.Location = new Point(157, 4);
        _applyBoardButton.Margin = new Padding(4);
        _applyBoardButton.Name = "_applyBoardButton";
        _applyBoardButton.Padding = new Padding(8, 3, 8, 3);
        _applyBoardButton.Size = new Size(79, 35);
        _applyBoardButton.TabIndex = 1;
        _applyBoardButton.Text = "Applica";
        _applyBoardButton.UseVisualStyleBackColor = false;
        // 
        // _startBoardButton
        // 
        _startBoardButton.AutoSize = true;
        _startBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _startBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _startBoardButton.FlatStyle = FlatStyle.Flat;
        _startBoardButton.ForeColor = Color.WhiteSmoke;
        _startBoardButton.Location = new Point(244, 4);
        _startBoardButton.Margin = new Padding(4);
        _startBoardButton.Name = "_startBoardButton";
        _startBoardButton.Padding = new Padding(8, 3, 8, 3);
        _startBoardButton.Size = new Size(75, 35);
        _startBoardButton.TabIndex = 2;
        _startBoardButton.Text = "Avvia";
        _startBoardButton.UseVisualStyleBackColor = false;
        // 
        // _pauseBoardButton
        // 
        _pauseBoardButton.AutoSize = true;
        _pauseBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _pauseBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _pauseBoardButton.FlatStyle = FlatStyle.Flat;
        _pauseBoardButton.ForeColor = Color.WhiteSmoke;
        _pauseBoardButton.Location = new Point(327, 4);
        _pauseBoardButton.Margin = new Padding(4);
        _pauseBoardButton.Name = "_pauseBoardButton";
        _pauseBoardButton.Padding = new Padding(8, 3, 8, 3);
        _pauseBoardButton.Size = new Size(75, 35);
        _pauseBoardButton.TabIndex = 3;
        _pauseBoardButton.Text = "Pausa";
        _pauseBoardButton.UseVisualStyleBackColor = false;
        // 
        // _resumeBoardButton
        // 
        _resumeBoardButton.AutoSize = true;
        _resumeBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _resumeBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _resumeBoardButton.FlatStyle = FlatStyle.Flat;
        _resumeBoardButton.ForeColor = Color.WhiteSmoke;
        _resumeBoardButton.Location = new Point(410, 4);
        _resumeBoardButton.Margin = new Padding(4);
        _resumeBoardButton.Name = "_resumeBoardButton";
        _resumeBoardButton.Padding = new Padding(8, 3, 8, 3);
        _resumeBoardButton.Size = new Size(85, 35);
        _resumeBoardButton.TabIndex = 4;
        _resumeBoardButton.Text = "Riprendi";
        _resumeBoardButton.UseVisualStyleBackColor = false;
        // 
        // _stopBoardButton
        // 
        _stopBoardButton.AutoSize = true;
        _stopBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _stopBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _stopBoardButton.FlatStyle = FlatStyle.Flat;
        _stopBoardButton.ForeColor = Color.WhiteSmoke;
        _stopBoardButton.Location = new Point(503, 4);
        _stopBoardButton.Margin = new Padding(4);
        _stopBoardButton.Name = "_stopBoardButton";
        _stopBoardButton.Padding = new Padding(8, 3, 8, 3);
        _stopBoardButton.Size = new Size(75, 35);
        _stopBoardButton.TabIndex = 5;
        _stopBoardButton.Text = "Ferma";
        _stopBoardButton.UseVisualStyleBackColor = false;
        // 
        // _restartBoardButton
        // 
        _restartBoardButton.AutoSize = true;
        _restartBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _restartBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _restartBoardButton.FlatStyle = FlatStyle.Flat;
        _restartBoardButton.ForeColor = Color.WhiteSmoke;
        _restartBoardButton.Location = new Point(586, 4);
        _restartBoardButton.Margin = new Padding(4);
        _restartBoardButton.Name = "_restartBoardButton";
        _restartBoardButton.Padding = new Padding(8, 3, 8, 3);
        _restartBoardButton.Size = new Size(76, 35);
        _restartBoardButton.TabIndex = 6;
        _restartBoardButton.Text = "Riavvia";
        _restartBoardButton.UseVisualStyleBackColor = false;
        // 
        // _saveBoardButton
        // 
        _saveBoardButton.AutoSize = true;
        _saveBoardButton.BackColor = Color.FromArgb(27, 34, 48);
        _saveBoardButton.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        _saveBoardButton.FlatStyle = FlatStyle.Flat;
        _saveBoardButton.ForeColor = Color.WhiteSmoke;
        _saveBoardButton.Location = new Point(670, 4);
        _saveBoardButton.Margin = new Padding(4);
        _saveBoardButton.Name = "_saveBoardButton";
        _saveBoardButton.Padding = new Padding(8, 3, 8, 3);
        _saveBoardButton.Size = new Size(121, 35);
        _saveBoardButton.TabIndex = 7;
        _saveBoardButton.Text = "Salva EEPROM";
        _saveBoardButton.UseVisualStyleBackColor = false;
        // 
        // _languageLabel
        // 
        _languageLabel.AutoSize = true;
        _languageLabel.Location = new Point(12, 11);
        _languageLabel.Margin = new Padding(12, 11, 3, 0);
        _languageLabel.Name = "_languageLabel";
        _languageLabel.Size = new Size(46, 17);
        _languageLabel.TabIndex = 0;
        _languageLabel.Text = "Lingua";
        // 
        // _channelsLabel
        // 
        _channelsLabel.Dock = DockStyle.Top;
        _channelsLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _channelsLabel.ForeColor = Color.FromArgb(137, 208, 244);
        _channelsLabel.Location = new Point(5, 5);
        _channelsLabel.Name = "_channelsLabel";
        _channelsLabel.Size = new Size(190, 36);
        _channelsLabel.TabIndex = 1;
        _channelsLabel.Text = "CANALI";
        _channelsLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // outer
        // 
        outer.ColumnCount = 1;
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        outer.Controls.Add(bars, 0, 0);
        outer.Controls.Add(vertical, 0, 1);
        outer.Controls.Add(statusBar, 0, 2);
        outer.Dock = DockStyle.Fill;
        outer.Location = new Point(0, 0);
        outer.Name = "outer";
        outer.Padding = new Padding(12);
        outer.RowCount = 3;
        outer.RowStyles.Add(new RowStyle());
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        outer.Size = new Size(1464, 911);
        outer.TabIndex = 0;
        // 
        // bars
        // 
        bars.AutoSize = true;
        bars.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        bars.ColumnCount = 1;
        bars.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bars.Controls.Add(header, 0, 0);
        bars.Controls.Add(hardware, 0, 1);
        bars.Controls.Add(_boardState, 0, 2);
        bars.Dock = DockStyle.Top;
        bars.Location = new Point(12, 12);
        bars.Margin = new Padding(0);
        bars.Name = "bars";
        bars.RowCount = 3;
        bars.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        bars.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        bars.RowStyles.Add(new RowStyle());
        bars.Size = new Size(1440, 109);
        bars.TabIndex = 0;
        // 
        // header
        // 
        header.ColumnCount = 2;
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle());
        header.Controls.Add(toolbar, 0, 0);
        header.Controls.Add(languagePanel, 1, 0);
        header.Dock = DockStyle.Fill;
        header.Location = new Point(0, 0);
        header.Margin = new Padding(0);
        header.Name = "header";
        header.RowCount = 1;
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        header.Size = new Size(1440, 44);
        header.TabIndex = 0;
        // 
        // toolbar
        // 
        toolbar.AutoScroll = true;
        toolbar.BackColor = Color.FromArgb(16, 21, 32);
        toolbar.Controls.Add(_newButton);
        toolbar.Controls.Add(_openButton);
        toolbar.Controls.Add(_saveButton);
        toolbar.Controls.Add(_saveAsButton);
        toolbar.Controls.Add(_connect);
        toolbar.Controls.Add(_live);
        toolbar.Controls.Add(_firmwareButton);
        toolbar.Dock = DockStyle.Fill;
        toolbar.Location = new Point(0, 0);
        toolbar.Margin = new Padding(0);
        toolbar.Name = "toolbar";
        toolbar.Size = new Size(1261, 44);
        toolbar.TabIndex = 0;
        toolbar.WrapContents = false;
        // 
        // languagePanel
        // 
        languagePanel.AutoSize = true;
        languagePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        languagePanel.Controls.Add(_languageLabel);
        languagePanel.Controls.Add(_language);
        languagePanel.Dock = DockStyle.Fill;
        languagePanel.Location = new Point(1261, 0);
        languagePanel.Margin = new Padding(0);
        languagePanel.Name = "languagePanel";
        languagePanel.Size = new Size(179, 44);
        languagePanel.TabIndex = 1;
        languagePanel.WrapContents = false;
        // 
        // hardware
        // 
        hardware.AutoScroll = true;
        hardware.Controls.Add(_hardwareTarget);
        hardware.Controls.Add(_applyBoardButton);
        hardware.Controls.Add(_startBoardButton);
        hardware.Controls.Add(_pauseBoardButton);
        hardware.Controls.Add(_resumeBoardButton);
        hardware.Controls.Add(_stopBoardButton);
        hardware.Controls.Add(_restartBoardButton);
        hardware.Controls.Add(_saveBoardButton);
        hardware.Dock = DockStyle.Fill;
        hardware.Location = new Point(0, 44);
        hardware.Margin = new Padding(0);
        hardware.Name = "hardware";
        hardware.Size = new Size(1440, 44);
        hardware.TabIndex = 1;
        hardware.WrapContents = false;
        // 
        // vertical
        // 
        vertical.BackColor = Color.FromArgb(16, 21, 32);
        vertical.Dock = DockStyle.Fill;
        vertical.Location = new Point(15, 124);
        vertical.Name = "vertical";
        vertical.Orientation = Orientation.Horizontal;
        // 
        // vertical.Panel1
        // 
        vertical.Panel1.Controls.Add(previewLayout);
        vertical.Panel1MinSize = 260;
        // 
        // vertical.Panel2
        // 
        vertical.Panel2.Controls.Add(bottom);
        vertical.Panel2MinSize = 60;
        vertical.Size = new Size(1434, 742);
        vertical.SplitterDistance = 457;
        vertical.SplitterWidth = 7;
        vertical.TabIndex = 1;
        // 
        // previewLayout
        // 
        previewLayout.ColumnCount = 2;
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350F));
        previewLayout.Controls.Add(_ledPreview, 1, 0);
        previewLayout.Controls.Add(scroll, 0, 0);
        previewLayout.Dock = DockStyle.Fill;
        previewLayout.Location = new Point(0, 0);
        previewLayout.Margin = new Padding(0);
        previewLayout.Name = "previewLayout";
        previewLayout.RowCount = 1;
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        previewLayout.Size = new Size(1434, 457);
        previewLayout.TabIndex = 0;
        // 
        // scroll
        // 
        scroll.AutoScroll = true;
        scroll.BackColor = Color.FromArgb(16, 21, 32);
        scroll.Controls.Add(_timeline);
        scroll.Dock = DockStyle.Fill;
        scroll.Location = new Point(0, 0);
        scroll.Margin = new Padding(0);
        scroll.Name = "scroll";
        scroll.Size = new Size(1084, 457);
        scroll.TabIndex = 1;
        // 
        // bottom
        // 
        bottom.BackColor = Color.FromArgb(16, 21, 32);
        bottom.Dock = DockStyle.Fill;
        bottom.Location = new Point(0, 0);
        bottom.Name = "bottom";
        // 
        // bottom.Panel1
        // 
        bottom.Panel1.Controls.Add(chPanel);
        bottom.Panel1MinSize = 165;
        // 
        // bottom.Panel2
        // 
        bottom.Panel2.Controls.Add(detail);
        bottom.Size = new Size(1434, 278);
        bottom.SplitterDistance = 200;
        bottom.SplitterWidth = 7;
        bottom.TabIndex = 0;
        // 
        // chPanel
        // 
        chPanel.BackColor = Color.FromArgb(27, 34, 48);
        chPanel.Controls.Add(_channels);
        chPanel.Controls.Add(_channelsLabel);
        chPanel.Dock = DockStyle.Fill;
        chPanel.Location = new Point(0, 0);
        chPanel.Name = "chPanel";
        chPanel.Padding = new Padding(5);
        chPanel.Size = new Size(200, 278);
        chPanel.TabIndex = 0;
        // 
        // detail
        // 
        detail.BackColor = Color.FromArgb(27, 34, 48);
        detail.Controls.Add(_grid);
        detail.Controls.Add(_stepEditor);
        detail.Controls.Add(rightTop);
        detail.Dock = DockStyle.Fill;
        detail.Location = new Point(0, 0);
        detail.Name = "detail";
        detail.Padding = new Padding(8);
        detail.Size = new Size(1227, 278);
        detail.TabIndex = 0;
        // 
        // rightTop
        // 
        rightTop.AutoScroll = true;
        rightTop.Controls.Add(_summary);
        rightTop.Controls.Add(_addStepButton);
        rightTop.Controls.Add(_deleteStepButton);
        rightTop.Controls.Add(_clearButton);
        rightTop.Controls.Add(_copyButton);
        rightTop.Controls.Add(_singleStepButton);
        rightTop.Controls.Add(_repeat);
        rightTop.Controls.Add(_destination);
        rightTop.Dock = DockStyle.Top;
        rightTop.Location = new Point(8, 8);
        rightTop.Name = "rightTop";
        rightTop.Size = new Size(1211, 88);
        rightTop.TabIndex = 2;
        // 
        // statusBar
        // 
        statusBar.BackColor = Color.FromArgb(16, 21, 32);
        statusBar.Controls.Add(_status);
        statusBar.Controls.Add(_cursor);
        statusBar.Dock = DockStyle.Fill;
        statusBar.Location = new Point(15, 872);
        statusBar.Name = "statusBar";
        statusBar.Size = new Size(1434, 24);
        statusBar.TabIndex = 2;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(16, 21, 32);
        ClientSize = new Size(1464, 911);
        Controls.Add(outer);
        Font = new Font("Segoe UI", 9.5F);
        ForeColor = Color.WhiteSmoke;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(1150, 760);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "OpenBrickLight Sequence Studio";
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_destination).EndInit();
        outer.ResumeLayout(false);
        outer.PerformLayout();
        bars.ResumeLayout(false);
        bars.PerformLayout();
        header.ResumeLayout(false);
        header.PerformLayout();
        toolbar.ResumeLayout(false);
        toolbar.PerformLayout();
        languagePanel.ResumeLayout(false);
        languagePanel.PerformLayout();
        hardware.ResumeLayout(false);
        hardware.PerformLayout();
        vertical.Panel1.ResumeLayout(false);
        vertical.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)vertical).EndInit();
        vertical.ResumeLayout(false);
        previewLayout.ResumeLayout(false);
        scroll.ResumeLayout(false);
        bottom.Panel1.ResumeLayout(false);
        bottom.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)bottom).EndInit();
        bottom.ResumeLayout(false);
        chPanel.ResumeLayout(false);
        detail.ResumeLayout(false);
        rightTop.ResumeLayout(false);
        rightTop.PerformLayout();
        statusBar.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TimelineControl _timeline = null!;
    private DataGridView _grid = null!;
    private ListBox _channels = null!;
    private Label _summary = null!;
    private Label _status = null!;
    private Label _cursor = null!;
    private NumericUpDown _destination = null!;
    private ComboBox _language = null!;
    private StepEditorControl _stepEditor = null!;
    private Label _repeat = null!;
    private LedPreviewControl _ledPreview = null!;
    private Button _connect = null!;
    private CheckBox _live = null!;
    private ComboBox _hardwareTarget = null!;
    private Label _boardState = null!;
    private Button _newButton = null!;
    private Button _openButton = null!;
    private Button _saveButton = null!;
    private Button _saveAsButton = null!;
    private Button _firmwareButton = null!;
    private Button _addStepButton = null!;
    private Button _deleteStepButton = null!;
    private Button _clearButton = null!;
    private Button _copyButton = null!;
    private Button _singleStepButton = null!;
    private Button _applyBoardButton = null!;
    private Button _startBoardButton = null!;
    private Button _pauseBoardButton = null!;
    private Button _resumeBoardButton = null!;
    private Button _stopBoardButton = null!;
    private Button _restartBoardButton = null!;
    private Button _saveBoardButton = null!;
    private Label _languageLabel = null!;
    private Label _channelsLabel = null!;
    private TableLayoutPanel outer = null!;
    private FlowLayoutPanel toolbar = null!;
    private FlowLayoutPanel languagePanel = null!;
    private TableLayoutPanel header = null!;
    private FlowLayoutPanel hardware = null!;
    private TableLayoutPanel bars = null!;
    private SplitContainer vertical = null!;
    private TableLayoutPanel previewLayout = null!;
    private Panel scroll = null!;
    private SplitContainer bottom = null!;
    private Panel chPanel = null!;
    private Panel detail = null!;
    private FlowLayoutPanel rightTop = null!;
    private Panel statusBar = null!;
    private DataGridViewTextBoxColumn stepColumn = null!;
    private DataGridViewTextBoxColumn startColumn = null!;
    private DataGridViewTextBoxColumn endColumn = null!;
    private DataGridViewTextBoxColumn durationColumn = null!;
    private DataGridViewTextBoxColumn positionColumn = null!;
    private DataGridViewTextBoxColumn modeColumn = null!;
    private DataGridViewTextBoxColumn parametersColumn = null!;
}
