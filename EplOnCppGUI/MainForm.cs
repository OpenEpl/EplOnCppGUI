using QIQI.EplOnCpp.Core;
using System;
using System.IO;
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
            var source = new EProjectFile.EProjectFile();
            source.Load(File.OpenRead(textBox1.Text));
            ProjectConverter.EocProjectType projectType;
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    projectType = ProjectConverter.EocProjectType.Windows;
                    break;

                case 1:
                    projectType = ProjectConverter.EocProjectType.Console;
                    break;

                default:
                    throw new Exception("请选择正确的项目类型");
            }
            if (source.ESystemInfo.FileType != 3)
            {
                throw new Exception("源文件应为ECom(*.ec)文件");
            }
            ProjectConverter.Convert(source, textBox2.Text, projectType);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            new LicenseForm().Show(this);
        }
    }
}