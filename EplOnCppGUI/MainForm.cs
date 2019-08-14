using QIQI.EplOnCpp.Core;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace QIQI.EplOnCpp.GUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ConvertButton_Click(object sender, EventArgs e)
        {
            var logWriter = new StringWriter();
            var logger = new StreamLoggerWithContext(logWriter, logWriter, false);
            try
            {
                if (!EocEnv.IsValid)
                {
                    logger.Error("环境变量EOC_HOME未正确配置");
                    return;
                }
                var srcFile = textBox1.Text;
                var destFile = textBox2.Text;
                var source = new EProjectFile.EProjectFile();
                source.Load(File.OpenRead(srcFile));
                ProjectConverter.EocProjectType projectType;
                switch (comboBox1.SelectedIndex)
                {
                    case 0:
                        projectType = ProjectConverter.EocProjectType.Windows;
                        break;

                    case 1:
                        projectType = ProjectConverter.EocProjectType.Console;
                        break;

                    case 2:
                        projectType = ProjectConverter.EocProjectType.Dll;
                        break;

                    default:
                        throw new Exception("请选择正确的项目类型");
                }
                if (source.ESystemInfo.FileType != 3 && !checkBox1.Checked)
                {
                    throw new Exception("源文件应为ECom(*.ec)文件");
                }
                new ProjectConverter(source, projectType, null, logger).Generate(destFile);
                logger.Info("操作完成");
            }
            catch (Exception exception)
            {
                logger.Error("处理程序不正常退出，异常信息：{0}", exception);
            }
            finally
            {
                MessageBox.Show(logWriter.ToString(), "Log");
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            var coreVersionInfo = Attribute.GetCustomAttribute(typeof(ProjectConverter).Assembly, typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;
            this.Text = string.Format("EplOnCpp.GUI (Core: v{0})", coreVersionInfo?.InformationalVersion ?? "Unknown");
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            new LicenseForm().Show(this);
        }
    }
}