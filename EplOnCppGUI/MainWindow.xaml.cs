using BlackFox.VsWhere;
using QIQI.CMakeCaller;
using QIQI.CMakeCaller.Kits;
using QIQI.EplOnCpp.Core;
using QIQI.WpfStepwiseLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace QIQI.EplOnCppGUI
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel = new MainViewModel();
        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
        public MainWindow()
        {
            ViewModel.CMakeKits = CMakeKitsController.GetKits();
            DataContext = ViewModel;
            InitializeComponent();

            MyLogView.DataSource = new StepwiseLog();
            MyLogView.DataSource.Steps.Add(new SpecificStepLog()
            {
                State = StepState.Success,
                Description = "欢迎使用",
                Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = Properties.Resources.Welcome
                }
            });
            MyLogView.DataSource.Steps.Add(new SpecificStepLog()
            {
                State = StepState.Success,
                Description = "投喂区",
                Content = new Image()
                {
                    Source = LoadImage(Properties.Resources.Donate),
                    MaxWidth = 200
                }
            });
            MyLogView.DataSource.Steps.Add(new SpecificStepLog()
            {
                State = StepState.Success,
                Description = "许可证",
                Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = Properties.Resources.License
                }
            });
        }

        private void RescanCMakeKits_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Thread(() =>
                {
                    MessageBox.Show("扫描过程可能出现无响应，请耐心等待1-2分钟的时间");
                }).Start();
                RescanCMakeKits.IsEnabled = false;
                DispatcherHelper.DoEvents();
                CMakeKitsController.ScanKits();
                ViewModel.CMakeKits = CMakeKitsController.GetKits();
                ViewModel.CMakeKit = ViewModel.CMakeKits.FirstOrDefault();
            }
            catch(Exception exception)
            {
                MessageBox.Show(this, $"扫描出错：{exception}", "扫描出错", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RescanCMakeKits.IsEnabled = true;
            }
        }

        private string GetDefaultOutputPath()
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(ViewModel.SourcePath),
                System.IO.Path.GetFileNameWithoutExtension(ViewModel.SourcePath) + "_EOC");
        }

        private string GetDefaultBuildPath()
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(ViewModel.SourcePath),
                System.IO.Path.GetFileNameWithoutExtension(ViewModel.SourcePath) + "_EOC",
                "build");
        }

        private void SelectSourcePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "EPL files|*.e;*.ec|All files|*"
            };
            bool writeDefaultOutputPath = string.IsNullOrWhiteSpace(ViewModel.OutputPath)
                || ViewModel.OutputPath == GetDefaultOutputPath();
            bool writeDefaultBuildPath = string.IsNullOrWhiteSpace(ViewModel.BuildPath)
                || ViewModel.BuildPath == GetDefaultBuildPath();
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                ViewModel.SourcePath = dialog.FileName;
                if (writeDefaultOutputPath)
                {
                    ViewModel.OutputPath = GetDefaultOutputPath();
                }
                if (writeDefaultBuildPath)
                {
                    ViewModel.BuildPath = GetDefaultBuildPath();
                }
            }
        }
        private async Task<bool> ExecuteJob(StepwiseLog log)
        {
            var eocLogWriter = new StringWriter();
            var eocLogger = new StreamLoggerWithContext(eocLogWriter, eocLogWriter, false);
            var step = new SpecificStepLog()
            {
                Description = "转换为C++/CMake工程",
            };
            log.Steps.Add(step);
            try
            {
                if (!EocEnv.IsValid)
                {
                    eocLogger.Error("环境变量EOC_HOME未正确配置");
                    step.State = StepState.Failed;
                }
                var source = new EProjectFile.EProjectFile();
                source.Load(File.OpenRead(ViewModel.SourcePath));
                if (source.ESystemInfo.FileType != 3 && !ViewModel.Force)
                {
                    eocLogger.Error("源文件应为ECom(*.ec)文件");
                    step.State = StepState.Failed;
                }
                else
                {
                    await Task.Run(() =>
                    {
                        new ProjectConverter(source, ViewModel.ProjectType, null, eocLogger).Generate(ViewModel.OutputPath);
                        eocLogger.Info("完成");
                    });
                }
            }
            catch (Exception exception)
            {
                eocLogger.Error("处理程序不正常退出，异常信息：{0}", exception);
                step.State = StepState.Failed;
            }
            step.Content = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                Text = eocLogWriter.ToString()
            };
            if (step.State == StepState.Failed)
            {
                return false;
            }
            step.State = StepState.Success;
            if (!ViewModel.Build)
            {
                return false;
            }
            step = new SpecificStepLog()
            {
                Description = "初始化CMake环境",
            };
            log.Steps.Add(step);
            if (CMakeEnv.DefaultInstance == null)
            {
                step.State = StepState.Failed;
                step.Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Error: 未找到CMake编译环境"
                };
                return false;
            }
            if (ViewModel.CMakeKit == null || ViewModel.ProjectConfig == null)
            {
                step.State = StepState.Failed;
                step.Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Error: 工具链参数不完整"
                };
                return false;
            }
            step.Content = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                Text = $"CMake Detected: {CMakeEnv.DefaultInstance.CMakeBin}"
            };
            CMakeKit cmakeKit;
            try
            {
                cmakeKit = new CMakeKit(ViewModel.CMakeKit);
            }
            catch (Exception)
            {
                step.State = StepState.Failed;
                step.Content = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = $"Error: 初始化工具链 (${ViewModel.CMakeKit?.Name}) 失败"
                };
                return false;
            }
            step.State = StepState.Success;
            var success = await RunProcessAsync(log, "配置CMake工程", startInfoHelper => cmakeKit.StartConfigure(
                CMakeEnv.DefaultInstance,
                ViewModel.OutputPath,
                ViewModel.BuildPath,
                new Dictionary<string, CMakeSetting>() {
                    {"CMAKE_BUILD_TYPE", new CMakeSetting(ViewModel.ProjectConfig, "STRING") }
                },
                startInfoHelper));
            if (!success)
            {
                return false;
            }
            success = await RunProcessAsync(log, "编译CMake工程", startInfoHelper => cmakeKit.StartBuild(
                CMakeEnv.DefaultInstance,
                ViewModel.BuildPath,
                new CMakeBuildConfig()
                {
                    Config = ViewModel.ProjectConfig
                },
                startInfoHelper));
            if (!success)
            {
                return false;
            }
            return true;
        }
        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            var log = new StepwiseLog();
            MyLogView.DataSource = log;
            ViewModel.JobState = StepState.Processing;
            var success = await ExecuteJob(log);
            ViewModel.JobState = success ? StepState.Success : StepState.Failed;
            if (success && ViewModel.RunAfterBuild)
            {
                var possibleExePaths = new string[] {
                    "main.exe",
                    $"{ViewModel.ProjectConfig}\\main.exe"
                };
                var exePath =
                    possibleExePaths.Select(x => Path.Combine(ViewModel.BuildPath, x)).FirstOrDefault(File.Exists);
                if (exePath != null)
                {
                    using var process = Process.Start(exePath);
                }
            }
        }
        private async Task<bool> RunProcessAsync(StepwiseLog log, string description, Func<CMakeKit.ModifyStartInfo, Process> createProcess)
        {
            var textBlock = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            };
            var step = new SpecificStepLog()
            {
                Description = description,
                Content = textBlock
            };
            log.Steps.Add(step);
            var helper = new ProcessOutputToLogHelper(textBlock);
            var process = createProcess(helper.ModifyStartInfo);
            helper.HandOverProcess(process);
            await Task.Run(() =>
            {
                process.WaitForExit();
            });
            step.State = process.ExitCode == 0 ? StepState.Success : StepState.Failed;
            return process.ExitCode == 0;
        }
    }
}
