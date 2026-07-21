using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Forms
{
    public sealed class JobQueueForm : Form
    {
        private readonly AppRuntime _runtime;
        private readonly BindingSource _queueBinding = new BindingSource();
        private readonly BindingSource _stepBinding = new BindingSource();
        private readonly DataGridView _queueGrid = new DataGridView();
        private readonly DataGridView _stepGrid = new DataGridView();
        private readonly Label _statusLabel = new Label();
        private readonly Label _lastRunLabel = new Label();
        private readonly Button _runJobsButton = new Button();
        private readonly ProgressBar _runJobsProgressBar = new ProgressBar();
        public JobQueueForm(AppRuntime runtime)
        {
            _runtime = runtime;
            Text = "Catalog / EFM Data Processing";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1220, 760);
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);

            Controls.Add(BuildLayout());
            Load += OnLoadForm;
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            try
            {
                Log.Information("JobQueueForm loading.");
                RefreshData();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to load job queue");
            }
        }

        private Control BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.Padding = new Padding(18);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 68F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));

            var title = new Label();
            title.Text = "CATALOG / EFM DATA PROCESSING";
            title.AutoSize = true;
            title.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.FromArgb(28, 57, 91);
            title.Padding = new Padding(0, 0, 0, 10);
            root.Controls.Add(title, 0, 0);

            ConfigureQueueGrid();
            ConfigureStepGrid();
            root.Controls.Add(_queueGrid, 0, 1);
            root.Controls.Add(_stepGrid, 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);

            return root;
        }

        private Control BuildFooter()
        {
            var footer = new TableLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.ColumnCount = 2;
            footer.RowCount = 1;
            footer.Padding = new Padding(0, 12, 0, 0);
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560F));

            var statusPanel = new TableLayoutPanel();
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.ColumnCount = 1;
            statusPanel.RowCount = 3;
            statusPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lastRunLabel.Text = "Last Run: none";
            _lastRunLabel.AutoSize = true;
            _lastRunLabel.Padding = new Padding(0, 4, 0, 4);
            _lastRunLabel.ForeColor = Color.FromArgb(35, 35, 35);

            _runJobsProgressBar.Dock = DockStyle.Top;
            _runJobsProgressBar.Visible = false;
            _runJobsProgressBar.Style = ProgressBarStyle.Marquee;
            _runJobsProgressBar.MarqueeAnimationSpeed = 30;
            _runJobsProgressBar.Height = 18;
            _runJobsProgressBar.Margin = new Padding(0, 4, 24, 6);

            _statusLabel.Text = "Currently Processing:";
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.AutoSize = false;
            _statusLabel.Padding = new Padding(0, 4, 0, 0);

            statusPanel.Controls.Add(_lastRunLabel, 0, 0);
            statusPanel.Controls.Add(_runJobsProgressBar, 0, 1);
            statusPanel.Controls.Add(_statusLabel, 0, 2);

            var buttonGrid = new TableLayoutPanel();
            buttonGrid.Dock = DockStyle.Fill;
            buttonGrid.ColumnCount = 3;
            buttonGrid.RowCount = 2;
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            buttonGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            buttonGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            //buttonGrid.Controls.Add(CreateButton("Check Jobs", CheckJobsClick), 0, 0);
            //buttonGrid.Controls.Add(CreateButton("Run Jobs", RunJobsClick), 1, 0);
            //buttonGrid.Controls.Add(CreateButton("Stop Jobs", StopJobsClick), 2, 0);
            //buttonGrid.Controls.Add(CreateButton("Cleanup / Done Queue", CleanupClick), 0, 1);
            //buttonGrid.Controls.Add(CreateButton("Archive Server", ArchiveServerClick), 1, 1);
            //buttonGrid.Controls.Add(CreateButton("Resume Jobs", ResumeJobsClick), 2, 1);

            ConfigureButton(_runJobsButton, "Run Jobs", RunJobsClick);
            buttonGrid.Controls.Add(_runJobsButton, 0, 0);
            buttonGrid.Controls.Add(CreateButton("Check Jobs", CheckJobsClick), 1, 0);
            buttonGrid.Controls.Add(CreateButton("Cleanup Archive", CleanupClick), 2, 0);
            buttonGrid.Controls.Add(CreateButton("Archive Server", ArchiveServerClick), 0, 1);
            buttonGrid.Controls.Add(CreateButton("Exit", ExitClick), 1, 1);                      
            //buttonGrid.Controls.Add(CreateButton("Resume Jobs", ResumeJobsClick), 2, 1);

            footer.Controls.Add(statusPanel, 0, 0);
            footer.Controls.Add(buttonGrid, 1, 0);
            return footer;
        }

        private void ExitClick(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to close Job Queue form");
            }
        }

        private void ConfigureQueueGrid()
        {
            _queueGrid.Dock = DockStyle.Fill;
            _queueGrid.ReadOnly = true;
            _queueGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _queueGrid.MultiSelect = false;
            _queueGrid.AutoGenerateColumns = false;
            _queueGrid.AllowUserToAddRows = false;
            _queueGrid.AllowUserToDeleteRows = false;
            _queueGrid.DataSource = _queueBinding;

            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Select", HeaderText = "Select", Width = 60 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobType", HeaderText = "Job Type", Width = 90 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "JobID", HeaderText = "Job ID", Width = 140 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WhlCode", HeaderText = "Whl Code", Width = 90 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "WhlDescription", HeaderText = "Description", Width = 220 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CatMonth", HeaderText = "Cat Month", Width = 90 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method1", HeaderText = "M1", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method2", HeaderText = "M2", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method3", HeaderText = "M3", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method4", HeaderText = "M4", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method5", HeaderText = "M5", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method6", HeaderText = "M6", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method7", HeaderText = "M7", Width = 45 });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Method8", HeaderText = "M8", Width = 45 });

            _queueGrid.SelectionChanged += QueueSelectionChanged;
        }

        private void ConfigureStepGrid()
        {
            _stepGrid.Dock = DockStyle.Fill;
            _stepGrid.ReadOnly = true;
            _stepGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _stepGrid.MultiSelect = false;
            _stepGrid.AutoGenerateColumns = false;
            _stepGrid.AllowUserToAddRows = false;
            _stepGrid.AllowUserToDeleteRows = false;
            _stepGrid.DataSource = _stepBinding;

            _stepGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MethodName", HeaderText = "Method", Width = 120 });
            _stepGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "State", HeaderText = "Select", Width = 80 });
            _stepGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Description", Width = 520, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }

        private void RefreshData()
        {
            var jobs = _runtime.JobProcessingService.LoadQueue();
            _queueBinding.DataSource = jobs.ToList();
            RefreshSteps();
            _statusLabel.Text = "Currently Processing: " + jobs.Count + " queue entries loaded.";
        }

        private void RefreshSteps()
        {
            var job = GetSelectedJob();
            var steps = _runtime.JobProcessingService.GetStepDefinitions(job)
                .Select(delegate(JobStepDefinition step)
                {
                    return new StepDisplay
                    {
                        MethodName = step.MethodName,
                        State = job == null ? " " : job.GetMethodValue(step.MethodName),
                        Description = step.Description
                    };
                })
                .ToList();

            _stepBinding.DataSource = steps;
        }

        private void ShowResult(OperationResult result)
        {
            _statusLabel.Text = "Currently Processing: " + result.Message;
            if (result.Success)
            {
                _lastRunLabel.Text = "Last Run: " + DateTime.Now.ToString("M/d/yyyy h:mm:ss tt");
            }

            MessageBox.Show(
                this,
                result.Message,
                result.Title,
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private JobRecord GetSelectedJob()
        {
            return _queueGrid.CurrentRow != null ? _queueGrid.CurrentRow.DataBoundItem as JobRecord : null;
        }

        private static Button CreateButton(string text, EventHandler onClick)
        {
            var button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(8, 4, 8, 4);
            button.Click += onClick;
            return button;
        }
        private static void ConfigureButton(Button button, string text, EventHandler onClick)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(8, 4, 8, 4);
            button.Click += onClick;
        }
        private void QueueSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                RefreshSteps();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Unable to refresh job steps");
            }
        }

        private async void RunJobsClick(object sender, EventArgs e)
        {
            _runJobsButton.Enabled = false;
            _runJobsProgressBar.Visible = true;
            _statusLabel.Text = "Currently Processing: Run Jobs is running...";

            try
            {
                Log.Information("Run Jobs clicked.");

                var result = await Task.Run(delegate
                {
                    return _runtime.JobProcessingService.RunJobs(false);
                });

                RefreshData();
                ShowResult(result);

                if (result.Success)
                {
                    _runJobsButton.Enabled = true;
                }
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Run Jobs failed");

                // Recommended so the user can retry after an error.
                _runJobsButton.Enabled = true;
            }
            finally
            {
                _runJobsProgressBar.Visible = false;
                _runJobsButton.Enabled = true;
            }
        }

        private void ResumeJobsClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Resume Jobs clicked.");
                var result = _runtime.JobProcessingService.RunJobs(true);
                RefreshData();
                ShowResult(result);
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Resume Jobs failed");
            }
        }

        private void CheckJobsClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Check Jobs clicked.");
                var result = _runtime.JobProcessingService.CheckJobs();
                RefreshData();
                ShowResult(result);
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Check Jobs failed");
            }
        }

        private void CleanupClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Cleanup Jobs clicked.");
                var result = _runtime.JobProcessingService.CleanupToDoneQueue();
                RefreshData();
                ShowResult(result);
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Cleanup Jobs failed");
            }
        }

        private void ArchiveServerClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Archive Server clicked.");
                using (var form = new ArchiveForm(_runtime))
                {

                    form.ShowDialog(this);
                }
                RefreshData();
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Archive Server failed");
            }
        }

        private void StopJobsClick(object sender, EventArgs e)
        {
            try
            {
                Log.Information("Stop Jobs clicked.");
                MessageBox.Show(this, "No job is currently running. Stop Jobs is included to match the original workflow.", "Stop Jobs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception oException)
            {
                Error.Continue(oException, "Stop Jobs failed");
            }
        }

        private sealed class StepDisplay
        {
            public string MethodName { get; set; }
            public string State { get; set; }
            public string Description { get; set; }
        }
    }
}
