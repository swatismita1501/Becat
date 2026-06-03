using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Forms
{
    public sealed class SetupJobForm : Form
    {
        private readonly AppRuntime _runtime;
        private readonly BindingSource _bindingSource = new BindingSource();
        private readonly ComboBox _jobTypeCombo = new ComboBox();
        private readonly ComboBox _wholesalerCombo = new ComboBox();
        private readonly ComboBox _catMonthCombo = new ComboBox();
        private readonly TextBox _descriptionText = new TextBox();
        private readonly TextBox _jobIdText = new TextBox();
        private readonly TextBox _efmIdText = new TextBox();
        private readonly DateTimePicker _fromDatePicker = new DateTimePicker();
        private readonly DateTimePicker _toDatePicker = new DateTimePicker();
        private readonly Button _newButton = new Button();
        private readonly Button _editButton = new Button();
        private readonly Button _saveButton = new Button();
        private readonly Button _deleteButton = new Button();
        private readonly Button _cancelButton = new Button();
        private readonly DataGridView _grid = new DataGridView();

        private List<JobRecord> _jobs = new List<JobRecord>();
        private JobRecord _workingCopy;
        private int _editingIndex = -1;
        private bool _isEditing;

        public SetupJobForm(AppRuntime runtime)
        {
            _runtime = runtime;
            Text = "Setup For Eagle Catalog Data Processing";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1180, 720);
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Controls.Add(BuildLayout());
            Load += OnLoadForm;
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            try
            {
                Log.Information("SetupJobForm loading.");
                RefreshData();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to load setup form");
            }
        }

        private Control BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.Padding = new Padding(18);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label();
            title.Text = "SETUP FOR EAGLE CATALOG DATA PROCESSING";
            title.Dock = DockStyle.Fill;
            title.AutoSize = true;
            title.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.FromArgb(28, 57, 91);
            title.Padding = new Padding(0, 0, 0, 10);
            root.Controls.Add(title, 0, 0);

            var content = new SplitContainer();
            content.Dock = DockStyle.Fill;
            content.SplitterDistance = 680;
            ConfigureGrid();
            content.Panel1.Controls.Add(_grid);
            content.Panel2.Controls.Add(BuildEditor());

            root.Controls.Add(content, 0, 1);
            root.Controls.Add(BuildButtons(), 0, 2);
            return root;
        }

        private Control BuildEditor()
        {
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 8;
            panel.Padding = new Padding(18, 8, 0, 0);
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            AddField(panel, 0, "Job Type", ConfigureCombo(_jobTypeCombo, new[] { "CAT", "EFM", "INH" }));
            AddField(panel, 1, "Wholesaler", ConfigureCombo(_wholesalerCombo, _runtime.JobSetupService.GetWholesalers().Select(item => item.WhlCode).ToArray()));
            AddField(panel, 2, "Description", ConfigureTextBox(_descriptionText, true));
            AddField(panel, 3, "Catalog Month", ConfigureCombo(_catMonthCombo, AppConstants.CatalogMonths));
            AddField(panel, 4, "Job ID", ConfigureTextBox(_jobIdText, true));
            AddField(panel, 5, "EFM ID", ConfigureTextBox(_efmIdText, true));
            AddField(panel, 6, "From Date", ConfigureDate(_fromDatePicker));
            AddField(panel, 7, "To Date", ConfigureDate(_toDatePicker));

            _jobTypeCombo.SelectedIndexChanged += EditorChanged;
            _wholesalerCombo.SelectedIndexChanged += EditorChanged;
            _catMonthCombo.SelectedIndexChanged += EditorChanged;
            _fromDatePicker.ValueChanged += FromDateChanged;

            return panel;
        }

        private Control BuildButtons()
        {
            var panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.AutoSize = true;
            panel.Padding = new Padding(0, 12, 0, 0);

            ConfigureCommandButton(_newButton, "New", NewClick);
            ConfigureCommandButton(_editButton, "Edit", EditClick);
            ConfigureCommandButton(_saveButton, "Save", SaveClick);
            ConfigureCommandButton(_deleteButton, "Delete", DeleteClick);
            ConfigureCommandButton(_cancelButton, "Cancel", CancelClick);

            var closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.AutoSize = true;
            closeButton.Padding = new Padding(12, 8, 12, 8);
            closeButton.Click += CloseClick;

            panel.Controls.Add(_newButton);
            panel.Controls.Add(_editButton);
            panel.Controls.Add(_saveButton);
            panel.Controls.Add(_deleteButton);
            panel.Controls.Add(_cancelButton);
            panel.Controls.Add(closeButton);
            return panel;
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.DataSource = _bindingSource;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Select", HeaderText = "Select", Width = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobType", HeaderText = "Job Type", Width = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobID", HeaderText = "Job ID", Width = 130 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WhlCode", HeaderText = "Whl Code", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WhlDescription", HeaderText = "Whl Description", Width = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CatMonth", HeaderText = "Cat Month", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EFMID", HeaderText = "EFM ID", Width = 80 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FromDate", HeaderText = "From Date", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ToDate", HeaderText = "To Date", Width = 100 });

            _grid.SelectionChanged += GridSelectionChanged;
        }

        private void RefreshData()
        {
            _jobs = _runtime.DataStore.LoadQueue().Select(item => item.Clone()).ToList();
            _bindingSource.DataSource = _jobs;

            if (_jobs.Count > 0)
            {
                _grid.Rows[0].Selected = true;
                LoadSelection();
            }
            else
            {
                PopulateEditor(null);
            }

            SetEditing(false);
        }

        private void BeginNew()
        {
            _workingCopy = _runtime.JobSetupService.BuildSuggestedJob("CAT", string.Empty, DateTime.Today.ToString("MMM").ToUpperInvariant());
            _editingIndex = -1;
            PopulateEditor(_workingCopy);
            SetEditing(true);
        }

        private void BeginEdit()
        {
            var current = GetSelectedJob();
            if (current == null)
            {
                MessageBox.Show(this, "Select a record to edit.", "Edit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _workingCopy = current.Clone();
            _editingIndex = GetSelectedIndex();
            PopulateEditor(_workingCopy);
            SetEditing(true);
        }

        private void SaveCurrent()
        {
            if (!_isEditing || _workingCopy == null)
            {
                MessageBox.Show(this, "Record not in edit mode.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ReadEditorInto(_workingCopy);
            _runtime.JobSetupService.RecalculateJob(_workingCopy);

            var validation = _runtime.JobSetupService.Validate(_workingCopy);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                MessageBox.Show(this, validation, "STOP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_editingIndex >= 0)
            {
                _jobs[_editingIndex] = _workingCopy.Clone();
            }
            else
            {
                _jobs.Add(_workingCopy.Clone());
            }

            _runtime.JobSetupService.SaveJobs(_jobs);
            RefreshData();
        }

        private void DeleteCurrent()
        {
            var index = GetSelectedIndex();
            if (index < 0)
            {
                return;
            }

            var confirm = MessageBox.Show(this, "Delete the selected queue record?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _jobs.RemoveAt(index);
            _runtime.JobSetupService.SaveJobs(_jobs);
            RefreshData();
        }

        private void CancelEdit()
        {
            SetEditing(false);
            LoadSelection();
        }

        private void LoadSelection()
        {
            if (_isEditing)
            {
                return;
            }

            PopulateEditor(GetSelectedJob());
        }

        private void Recalculate()
        {
            if (!_isEditing)
            {
                return;
            }

            if (_workingCopy == null)
            {
                _workingCopy = new JobRecord();
            }

            ReadEditorInto(_workingCopy);
            _runtime.JobSetupService.RecalculateJob(_workingCopy);
            PopulateEditor(_workingCopy);
        }

        private void PopulateEditor(JobRecord job)
        {
            _jobTypeCombo.Text = job == null ? string.Empty : job.JobType;
            _wholesalerCombo.Text = job == null ? string.Empty : job.WhlCode;
            _descriptionText.Text = job == null ? string.Empty : job.WhlDescription;
            _catMonthCombo.Text = job == null ? string.Empty : job.CatMonth;
            _jobIdText.Text = job == null ? string.Empty : job.JobID;
            _efmIdText.Text = job == null ? string.Empty : job.EFMID;

            _fromDatePicker.Checked = job != null && job.FromDate.HasValue;
            _fromDatePicker.Value = job != null && job.FromDate.HasValue ? job.FromDate.Value : DateTime.Today;
            _toDatePicker.Checked = job != null && job.ToDate.HasValue;
            _toDatePicker.Value = job != null && job.ToDate.HasValue ? job.ToDate.Value : DateTime.Today;
        }

        private void ReadEditorInto(JobRecord job)
        {
            job.JobType = _jobTypeCombo.Text;
            job.WhlCode = _wholesalerCombo.Text;
            job.CatMonth = _catMonthCombo.Text;
            job.WhlDescription = _descriptionText.Text;
            job.JobID = _jobIdText.Text;
            job.EFMID = _efmIdText.Text;
            job.FromDate = _fromDatePicker.Checked ? _fromDatePicker.Value.Date : (DateTime?)null;
            job.ToDate = _toDatePicker.Checked ? _toDatePicker.Value.Date : (DateTime?)null;
            if (string.IsNullOrWhiteSpace(job.Select))
            {
                job.Select = "1";
            }
        }

        private void SetEditing(bool isEditing)
        {
            _isEditing = isEditing;
            var editors = new Control[] { _jobTypeCombo, _wholesalerCombo, _catMonthCombo, _fromDatePicker, _toDatePicker };
            foreach (var control in editors)
            {
                control.Enabled = isEditing;
            }

            _saveButton.Enabled = isEditing;
            _cancelButton.Enabled = isEditing;
            _newButton.Enabled = !isEditing;
            _editButton.Enabled = !isEditing;
            _deleteButton.Enabled = !isEditing;
            _grid.Enabled = !isEditing;
        }

        private JobRecord GetSelectedJob()
        {
            var index = GetSelectedIndex();
            return index >= 0 && index < _jobs.Count ? _jobs[index] : null;
        }

        private int GetSelectedIndex()
        {
            return _grid.CurrentRow == null ? -1 : _grid.CurrentRow.Index;
        }

        private static void AddField(TableLayoutPanel panel, int row, string label, Control editor)
        {
            var fieldLabel = new Label();
            fieldLabel.Text = label;
            fieldLabel.AutoSize = true;
            fieldLabel.Margin = new Padding(0, 10, 10, 10);
            fieldLabel.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(fieldLabel, 0, row);
            panel.Controls.Add(editor, 1, row);
        }

        private static ComboBox ConfigureCombo(ComboBox comboBox, string[] values)
        {
            comboBox.Dock = DockStyle.Top;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Items.AddRange(values);
            comboBox.Margin = new Padding(0, 6, 0, 6);
            return comboBox;
        }

        private static TextBox ConfigureTextBox(TextBox textBox, bool readOnly)
        {
            textBox.Dock = DockStyle.Top;
            textBox.ReadOnly = readOnly;
            textBox.Margin = new Padding(0, 6, 0, 6);
            return textBox;
        }

        private static DateTimePicker ConfigureDate(DateTimePicker picker)
        {
            picker.Dock = DockStyle.Top;
            picker.Format = DateTimePickerFormat.Short;
            picker.ShowCheckBox = true;
            picker.Margin = new Padding(0, 6, 0, 6);
            return picker;
        }

        private static void ConfigureCommandButton(Button button, string text, EventHandler clickHandler)
        {
            button.Text = text;
            button.AutoSize = true;
            button.Padding = new Padding(12, 8, 12, 8);
            button.Click += clickHandler;
        }

        private void EditorChanged(object sender, EventArgs e)
        {
            try
            {
                Recalculate();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to recalculate job setup");
            }
        }

        private void FromDateChanged(object sender, EventArgs e)
        {
            if (_isEditing && _workingCopy != null && string.Equals(_jobTypeCombo.Text, "CAT", StringComparison.OrdinalIgnoreCase))
            {
                _workingCopy.FromDate = _fromDatePicker.Checked ? _fromDatePicker.Value.Date : (DateTime?)null;
            }
        }

        private void NewClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Setup new clicked.");
                BeginNew();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to start new job");
            }
        }

        private void EditClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Setup edit clicked.");
                BeginEdit();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to edit job");
            }
        }

        private void SaveClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Setup save clicked.");
                SaveCurrent();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to save job");
            }
        }

        private void DeleteClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Setup delete clicked.");
                DeleteCurrent();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to delete job");
            }
        }

        private void CancelClick(object sender, EventArgs e)
        {
            try
            {
                CancelEdit();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to cancel edit");
            }
        }

        private void CloseClick(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to close setup form");
            }
        }

        private void GridSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                LoadSelection();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to load selected job");
            }
        }
    }
}
