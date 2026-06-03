using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Forms
{
    public sealed class ArchiveForm : Form
    {
        private readonly AppRuntime _runtime;
        private readonly BindingSource _doneBinding = new BindingSource();
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _remoteRawText = new TextBox();
        private readonly TextBox _remoteArchiveText = new TextBox();
        private readonly TextBox _zipArchiveText = new TextBox();
        private readonly Button _editButton = new Button();
        private readonly Button _saveButton = new Button();
        private bool _isEditing;

        public ArchiveForm(AppRuntime runtime)
        {
            _runtime = runtime;
            Text = "Eagle Catalog / EFM Monthly Archive";
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
                Log.Information("ArchiveForm loading.");
                RefreshData();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to load archive form");
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
            title.Text = "EAGLE CATALOG / EFM MONTHLY ARCHIVE";
            title.AutoSize = true;
            title.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.FromArgb(28, 57, 91);
            title.Padding = new Padding(0, 0, 0, 10);
            root.Controls.Add(title, 0, 0);

            var split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 740;
            ConfigureGrid();
            split.Panel1.Controls.Add(_grid);
            split.Panel2.Controls.Add(BuildConfigEditor());

            root.Controls.Add(split, 0, 1);
            root.Controls.Add(BuildFooter(), 0, 2);

            return root;
        }

        private Control BuildConfigEditor()
        {
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 1;
            panel.RowCount = 6;
            panel.Padding = new Padding(18, 8, 0, 0);

            var heading = new Label();
            heading.Text = "Server Configuration";
            heading.AutoSize = true;
            heading.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            heading.Padding = new Padding(0, 0, 0, 12);
            panel.Controls.Add(heading);

            panel.Controls.Add(CreateLabeledText("Remote Raw", _remoteRawText));
            panel.Controls.Add(CreateLabeledText("Remote Archive", _remoteArchiveText));
            panel.Controls.Add(CreateLabeledText("Zip Archive", _zipArchiveText));

            var buttonRow = new FlowLayoutPanel();
            buttonRow.AutoSize = true;
            buttonRow.FlowDirection = FlowDirection.LeftToRight;
            buttonRow.Padding = new Padding(0, 12, 0, 0);

            _editButton.Text = "Edit";
            _editButton.AutoSize = true;
            _editButton.Padding = new Padding(12, 8, 12, 8);
            _editButton.Click += EditClick;

            _saveButton.Text = "Save";
            _saveButton.AutoSize = true;
            _saveButton.Padding = new Padding(12, 8, 12, 8);
            _saveButton.Click += SaveClick;

            buttonRow.Controls.Add(_editButton);
            buttonRow.Controls.Add(_saveButton);
            panel.Controls.Add(buttonRow);

            return panel;
        }

        private Control BuildFooter()
        {
            var panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.LeftToRight;
            panel.Padding = new Padding(0, 12, 0, 0);
            panel.Controls.Add(CreateButton("Archive Server", ArchiveClick));
            panel.Controls.Add(CreateButton("Refresh", RefreshClick));
            panel.Controls.Add(CreateButton("Close", CloseClick));
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
            _grid.DataSource = _doneBinding;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Select", HeaderText = "Select", Width = 60 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobType", HeaderText = "Job Type", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobID", HeaderText = "Job ID", Width = 160 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WhlCode", HeaderText = "Whl Code", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WhlDescription", HeaderText = "Description", Width = 220 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CatMonth", HeaderText = "Cat Month", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EFMID", HeaderText = "EFM ID", Width = 80 });
        }

        private void RefreshData()
        {
            _doneBinding.DataSource = _runtime.ArchiveService.LoadDoneQueue().ToList();
            LoadConfig();
            SetEditing(false);
        }

        private void LoadConfig()
        {
            var config = _runtime.ArchiveService.LoadConfig();
            _remoteRawText.Text = config.RemoteRaw;
            _remoteArchiveText.Text = config.RemoteArchive;
            _zipArchiveText.Text = config.ZipArchive;
        }

        private void SaveConfig()
        {
            if (!_isEditing)
            {
                MessageBox.Show(this, "Record not in edit mode.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _runtime.ArchiveService.SaveConfig(new ArchiveConfig
            {
                RemoteRaw = _remoteRawText.Text,
                RemoteArchive = _remoteArchiveText.Text,
                ZipArchive = _zipArchiveText.Text
            });

            SetEditing(false);
            MessageBox.Show(this, "Archive configuration saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RunArchive()
        {
            var result = _runtime.ArchiveService.ArchiveReadyJobs();
            RefreshData();
            MessageBox.Show(this, result.Message, result.Title, MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void SetEditing(bool enabled)
        {
            _isEditing = enabled;
            _remoteRawText.ReadOnly = !enabled;
            _remoteArchiveText.ReadOnly = !enabled;
            _zipArchiveText.ReadOnly = !enabled;
            _editButton.Enabled = !enabled;
            _saveButton.Enabled = enabled;
        }

        private static Control CreateLabeledText(string labelText, TextBox textBox)
        {
            textBox.Dock = DockStyle.Top;
            textBox.ReadOnly = true;
            textBox.Margin = new Padding(0, 4, 0, 10);

            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.ColumnCount = 1;
            panel.RowCount = 2;
            panel.AutoSize = true;

            var label = new Label();
            label.Text = labelText;
            label.AutoSize = true;
            panel.Controls.Add(label, 0, 0);
            panel.Controls.Add(textBox, 0, 1);

            return panel;
        }

        private static Button CreateButton(string text, EventHandler onClick)
        {
            var button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.Padding = new Padding(12, 8, 12, 8);
            button.Click += onClick;
            return button;
        }

        private void EditClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Archive edit clicked.");
                SetEditing(true);
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to edit archive settings");
            }
        }

        private void SaveClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Archive save clicked.");
                SaveConfig();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to save archive settings");
            }
        }

        private void ArchiveClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Archive run clicked.");
                RunArchive();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to run archive");
            }
        }

        private void RefreshClick(object sender, EventArgs e)
        {
            try
            {
                RefreshData();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to refresh archive data");
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
                Error.Continue(oException, "Unable to close archive form");
            }
        }
    }
}
