using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace WindowsPhoneNextBuildTool
{
    public partial class MainWindow : Window
    {
        private string projectRoot;
        private CancellationTokenSource? cancellationTokenSource;
        private bool isBuilding = false;

        public MainWindow()
        {
            InitializeComponent();

            // Get project root directory (parent of BuildTool)
            projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

            LogMessage($"Windows Phone Next Build Tool v1.0");
            LogMessage($"Project Directory: {projectRoot}");
            LogMessage("");
        }

        private void BrowseIsoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Windows 11 ISO",
                Filter = "ISO Files (*.iso)|*.iso|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsoPathTextBox.Text = openFileDialog.FileName;
                DownloadIsoCheckBox.IsChecked = false;
                LogMessage($"ISO selected: {openFileDialog.FileName}");
            }
        }

        private async void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBuilding)
            {
                MessageBox.Show("A build is already in progress.", "Build In Progress",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Validate prerequisites
            if (!BuildAppsCheckBox.IsChecked == true && !CreateImageCheckBox.IsChecked == true)
            {
                MessageBox.Show("Please select at least one build option.", "No Options Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CreateImageCheckBox.IsChecked == true &&
                string.IsNullOrWhiteSpace(IsoPathTextBox.Text) &&
                DownloadIsoCheckBox.IsChecked != true)
            {
                MessageBox.Show("Please provide a Windows 11 ISO or enable automatic download.",
                    "ISO Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Start build process
            isBuilding = true;
            cancellationTokenSource = new CancellationTokenSource();

            // Update UI
            BuildButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            BrowseIsoButton.IsEnabled = false;
            CleanBuildCheckBox.IsEnabled = false;
            BuildAppsCheckBox.IsEnabled = false;
            DownloadDriversCheckBox.IsEnabled = false;
            CreateImageCheckBox.IsEnabled = false;
            CreateUsbCheckBox.IsEnabled = false;
            DownloadIsoCheckBox.IsEnabled = false;

            MainProgressBar.Value = 0;
            LogTextBlock.Text = "";

            try
            {
                await RunBuildProcess(cancellationTokenSource.Token);

                UpdateStatus("Build completed successfully!", Brushes.LimeGreen);
                MainProgressBar.Value = 100;

                MessageBox.Show("Build process completed successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Build cancelled by user", Brushes.Orange);
                LogMessage("Build process was cancelled.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Build failed with errors", Brushes.Red);
                LogMessage($"ERROR: {ex.Message}");

                MessageBox.Show($"Build failed:\n\n{ex.Message}", "Build Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Reset UI
                isBuilding = false;
                BuildButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                BrowseIsoButton.IsEnabled = true;
                CleanBuildCheckBox.IsEnabled = true;
                BuildAppsCheckBox.IsEnabled = true;
                DownloadDriversCheckBox.IsEnabled = true;
                CreateImageCheckBox.IsEnabled = true;
                CreateUsbCheckBox.IsEnabled = true;
                DownloadIsoCheckBox.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null && isBuilding)
            {
                UpdateStatus("Cancelling build...", Brushes.Orange);
                cancellationTokenSource.Cancel();
            }
        }

        private void OpenOutputButton_Click(object sender, RoutedEventArgs e)
        {
            string outputPath = Path.Combine(projectRoot, "Output");
            OpenFolder(outputPath);
        }

        private void OpenBuildFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string buildPath = Path.Combine(projectRoot, "ImageWork");
            if (Directory.Exists(buildPath))
            {
                OpenFolder(buildPath);
            }
            else
            {
                OpenFolder(projectRoot);
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
                else
                {
                    MessageBox.Show($"Folder does not exist:\n{path}", "Folder Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RunBuildProcess(CancellationToken cancellationToken)
        {
            var tasks = new List<(string Name, Func<CancellationToken, Task> Task, bool Enabled)>();

            // Build the task list based on selected options
            if (CleanBuildCheckBox.IsChecked == true)
            {
                tasks.Add(("Clean", CleanBuildAsync, true));
            }

            if (BuildAppsCheckBox.IsChecked == true)
            {
                tasks.Add(("Build Applications", BuildApplicationsAsync, true));
            }

            if (DownloadDriversCheckBox.IsChecked == true)
            {
                tasks.Add(("Download Drivers", DownloadDriversAsync, true));
            }

            if (CreateImageCheckBox.IsChecked == true)
            {
                // Download ISO if needed
                if (DownloadIsoCheckBox.IsChecked == true &&
                    (string.IsNullOrWhiteSpace(IsoPathTextBox.Text) || IsoPathTextBox.Text.Contains("No ISO selected")))
                {
                    tasks.Add(("Download ISO", DownloadIsoAsync, true));
                }

                tasks.Add(("Create Image", CreateImageAsync, true));
            }

            if (CreateUsbCheckBox.IsChecked == true)
            {
                tasks.Add(("Generate USB Script", GenerateUsbScriptAsync, true));
            }

            // Execute tasks sequentially
            int totalTasks = tasks.Count;
            int currentTask = 0;

            foreach (var (name, task, enabled) in tasks)
            {
                if (!enabled) continue;

                cancellationToken.ThrowIfCancellationRequested();

                currentTask++;
                double progressPercentage = ((currentTask - 1) / (double)totalTasks) * 100;

                UpdateStatus($"[{currentTask}/{totalTasks}] {name}...", Brushes.DeepSkyBlue);
                UpdateProgress(progressPercentage);

                LogMessage("");
                LogMessage($"═══════════════════════════════════════════════════");
                LogMessage($"  STEP {currentTask}/{totalTasks}: {name.ToUpper()}");
                LogMessage($"═══════════════════════════════════════════════════");
                LogMessage("");

                await task(cancellationToken);

                UpdateProgress(((double)currentTask / totalTasks) * 100);
            }
        }

        private async Task CleanBuildAsync(CancellationToken cancellationToken)
        {
            string scriptPath = Path.Combine(projectRoot, "Build", "build-all.ps1");
            await RunPowerShellScript(scriptPath, "-Clean", cancellationToken);
        }

        private async Task BuildApplicationsAsync(CancellationToken cancellationToken)
        {
            string scriptPath = Path.Combine(projectRoot, "Build", "build-all.ps1");
            string args = "-Configuration Release";

            await RunPowerShellScript(scriptPath, args, cancellationToken);
        }

        private async Task DownloadDriversAsync(CancellationToken cancellationToken)
        {
            string scriptPath = Path.Combine(projectRoot, "Build", "download-drivers.ps1");
            await RunPowerShellScript(scriptPath, "", cancellationToken);
        }

        private async Task DownloadIsoAsync(CancellationToken cancellationToken)
        {
            string scriptPath = Path.Combine(projectRoot, "Build", "download-iso.ps1");
            await RunPowerShellScript(scriptPath, "", cancellationToken);

            // Update ISO path after download
            string isoPath = Path.Combine(projectRoot, "Windows11.iso");
            if (File.Exists(isoPath))
            {
                await Dispatcher.InvokeAsync(() => IsoPathTextBox.Text = isoPath);
            }
        }

        private async Task CreateImageAsync(CancellationToken cancellationToken)
        {
            string scriptPath = Path.Combine(projectRoot, "Build", "create-image.ps1");
            string args = "";

            // Add ISO path if provided
            if (!string.IsNullOrWhiteSpace(IsoPathTextBox.Text) &&
                !IsoPathTextBox.Text.Contains("No ISO selected"))
            {
                args = $"-IsoPath \"{IsoPathTextBox.Text}\"";
            }

            await RunPowerShellScript(scriptPath, args, cancellationToken);
        }

        private async Task GenerateUsbScriptAsync(CancellationToken cancellationToken)
        {
            // The create-image.ps1 script already generates create-usb.ps1
            // Just verify it exists
            await Task.Run(() =>
            {
                string usbScriptPath = Path.Combine(projectRoot, "ImageWork", "create-usb.ps1");
                if (File.Exists(usbScriptPath))
                {
                    LogMessage($"USB creation script generated: {usbScriptPath}");
                }
                else
                {
                    LogMessage("USB creation script will be generated after image creation.");
                }
            }, cancellationToken);
        }

        private async Task RunPowerShellScript(string scriptPath, string arguments, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var runspace = RunspaceFactory.CreateRunspace();
                    runspace.Open();

                    using var pipeline = runspace.CreatePipeline();

                    // Set execution policy for this session
                    pipeline.Commands.AddScript("Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force");

                    // Change to project directory
                    pipeline.Commands.AddScript($"Set-Location '{projectRoot}'");

                    // Run the script
                    string command = $"& '{scriptPath}' {arguments}";
                    pipeline.Commands.AddScript(command);

                    // Capture output
                    var results = pipeline.Invoke();

                    // Process output
                    foreach (var result in results)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (result != null)
                        {
                            string output = result.ToString();
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                LogMessage(output);
                            }
                        }
                    }

                    // Check for errors
                    if (pipeline.Error.Count > 0)
                    {
                        foreach (var error in pipeline.Error.ReadToEnd())
                        {
                            string errorMsg = error.ToString();
                            if (!string.IsNullOrWhiteSpace(errorMsg))
                            {
                                LogMessage($"ERROR: {errorMsg}");
                            }
                        }
                    }

                    // Check pipeline state
                    if (pipeline.PipelineStateInfo.State == PipelineState.Failed)
                    {
                        throw new Exception($"PowerShell script failed: {scriptPath}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR executing script: {ex.Message}");
                    throw;
                }
            }, cancellationToken);
        }

        private void UpdateStatus(string message, Brush? color = null)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = message;
                StatusTextBlock.Foreground = color ?? (Brush)FindResource("TextPrimaryBrush");
            });
        }

        private void UpdateProgress(double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                MainProgressBar.Value = percentage;
                ProgressTextBlock.Text = $"{percentage:F1}%";
            });
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}";

                if (string.IsNullOrEmpty(LogTextBlock.Text) || LogTextBlock.Text == "Build log will appear here...")
                {
                    LogTextBlock.Text = logEntry;
                }
                else
                {
                    LogTextBlock.Text += Environment.NewLine + logEntry;
                }

                // Auto-scroll to bottom
                if (LogTextBlock.Parent is Border border && border.Parent is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToEnd();
                }
            });
        }
    }
}
