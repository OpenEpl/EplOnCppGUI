using QIQI.CMakeCaller.Kits;
using QIQI.EplOnCpp.Core;
using QIQI.WpfStepwiseLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace QIQI.EplOnCppGUI
{
    class MainViewModel : INotifyPropertyChanged
    {
        public static Dictionary<string, ProjectConverter.EocProjectType> CommonProjectType { get; }
            = new Dictionary<string, ProjectConverter.EocProjectType>() {
                {"WinApp", ProjectConverter.EocProjectType.Windows },
                {"WinCosole", ProjectConverter.EocProjectType.Console },
                {"WinDll", ProjectConverter.EocProjectType.Dll },
            };
        public static ReadOnlyCollection<string> CommonProjectConfig { get; } = Array.AsReadOnly(new string[] {
            "Debug",
            "RelWithDebInfo",
            "Release",
            "MinSizeRel"
        });
        public static string CoreVersion { get; } =
             ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                 typeof(ProjectConverter).Assembly, 
                 typeof(AssemblyInformationalVersionAttribute)))
            ?.InformationalVersion
            ?? "Unknown";
        public static string GuiVersion { get; } =
             ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                 typeof(MainViewModel).Assembly,
                 typeof(AssemblyInformationalVersionAttribute)))
            ?.InformationalVersion
            ?? "Unknown";

#pragma warning disable CS0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067
        public string SourcePath { get; set; }
        public string OutputPath { get; set; }
        public string BuildPath { get; set; }
        public ProjectConverter.EocProjectType ProjectType { get; set; }
        public ReadOnlyCollection<CMakeKitInfo> CMakeKits { get; set; }
        public CMakeKitInfo CMakeKit { get; set; }
        public bool RunAfterBuild { get; set; } = true;
        public bool Force { get; set; } = false;
        public string ProjectConfig = CommonProjectConfig[0];
        public bool Build { get; set; } = true;
        public StepState JobState { get; set; } = StepState.Success;
    }
}
