using QIQI.WpfStepwiseLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QIQI.EplOnCppGUI
{
    class ProcessOutputToLogHelper
    {
        private Process process;
        private BackgroundWorker outputWorker = new BackgroundWorker();
        private BackgroundWorker errorWorker = new BackgroundWorker();
        private TextBlock target;

        public ProcessOutputToLogHelper(TextBlock target)
        {
            this.target = target;

            outputWorker.WorkerReportsProgress = true;
            outputWorker.WorkerSupportsCancellation = true;
            outputWorker.DoWork += Worker_DoWork;
            outputWorker.ProgressChanged += OutputWorker_ProgressChanged;

            errorWorker.WorkerReportsProgress = true;
            errorWorker.WorkerSupportsCancellation = true;
            errorWorker.DoWork += Worker_DoWork;
            errorWorker.ProgressChanged += ErrorWorker_ProgressChanged;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var reader = e.Argument as TextReader;
            while ((sender as BackgroundWorker).CancellationPending == false)
            {
                int count;
                var buffer = new char[1024];
                do
                {
                    var builder = new StringBuilder();
                    count = reader.Read(buffer, 0, 1024);
                    builder.Append(buffer, 0, count);
                    (sender as BackgroundWorker).ReportProgress(0, builder.ToString());
                } while (count > 0);

                System.Threading.Thread.Sleep(200);
            }
        }

        private void OutputWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string x)
            {
                target.Inlines.Add(new Run(x));
            }
        }

        private void ErrorWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string x)
            {
                target.Inlines.Add(new Run(x) {
                    Foreground = Brushes.Red
                });
            }
        }

        public void ModifyStartInfo(ProcessStartInfo startInfo)
        {
            startInfo.UseShellExecute = false;
            startInfo.ErrorDialog = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
        }

        public void HandOverProcess(Process process)
        {
            this.process = process;
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;
            outputWorker.RunWorkerAsync(TextReader.Synchronized(process.StandardOutput));
            errorWorker.RunWorkerAsync(TextReader.Synchronized(process.StandardError));
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            outputWorker.CancelAsync();
            errorWorker.CancelAsync();
            process = null;
        }
    }
}
