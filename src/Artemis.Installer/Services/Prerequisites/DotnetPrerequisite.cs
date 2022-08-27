using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Artemis.Installer.Utilities;

namespace Artemis.Installer.Services.Prerequisites
{
    public class DotnetPrerequisite : IPrerequisite
    {
        protected virtual void OnDownloadProgressUpdated()
        {
            DownloadProgressUpdated?.Invoke(this, EventArgs.Empty);
        }

        public string Title => ".NET 6 Desktop runtime x64";
        public string Description => "The .NET 6 Desktop runtime is required for Artemis to run, the download is about 50 MB";
        public string DownloadUrl => "https://aka.ms/dotnet/6.0/windowsdesktop-runtime-win-x64.exe";

        public bool IsDownloading { get; set; }
        public bool IsInstalling { get; set; }

        public long DownloadCurrentBytes { get; private set; }
        public long DownloadTotalBytes { get; private set; }
        public float DownloadPercentage { get; private set; }

        public bool IsMet()
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "dotnet.exe",
                Arguments = "--list-runtimes",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            try
            {
                Process process = Process.Start(processInfo);
                if (process == null)
                    return false;

                process.WaitForExit();
                string versions = process.StandardOutput.ReadToEnd();

                // Any version between 6 and 9 is fine for now
                MatchCollection matches = Regex.Matches(versions, @"Microsoft\.WindowsDesktop\.App ([6-9].\d*.\d*).*");

                return matches.Count > 0;
            }
            catch (Win32Exception e)
            {
                // File not found
                if (e.NativeErrorCode == 2)
                    return false;
                throw;
            }
        }

        public async Task Install(string file)
        {
            await ProcessUtilities.RunProcessAsync(file, "-passive");
        }

        public void ReportProgress(long currentBytes, long totalBytes, float percentage)
        {
            DownloadCurrentBytes = currentBytes;
            DownloadTotalBytes = totalBytes;
            DownloadPercentage = percentage;
            OnDownloadProgressUpdated();
        }

        public event EventHandler DownloadProgressUpdated;
    }
}