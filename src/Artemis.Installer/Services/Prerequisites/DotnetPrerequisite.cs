using System;
using System.Threading.Tasks;
using Artemis.Installer.Utilities;
using Microsoft.Win32;

namespace Artemis.Installer.Services.Prerequisites
{
    public class DotnetPrerequisite : IPrerequisite
    {
        protected virtual void OnDownloadProgressUpdated()
        {
            DownloadProgressUpdated?.Invoke(this, EventArgs.Empty);
        }

        public string Title => ".NET 5 runtime x64";
        public string Description => "The .NET 5 runtime is required for Artemis to run, the download is about 50 MB";
        public string DownloadUrl => "https://download.visualstudio.microsoft.com/download/pr/2b83d30e-5c86-4d37-a1a6-582e22ac07b2/c7b1b7e21761bbfb7b9951f5b258806e/windowsdesktop-runtime-5.0.7-win-x64.exe";

        public bool IsDownloading { get; set; }
        public bool IsInstalling { get; set; }

        public long DownloadCurrentBytes { get; private set; }
        public long DownloadTotalBytes { get; private set; }
        public float DownloadPercentage { get; private set; }

        public bool IsMet()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost");
            object versionValue = key?.GetValue("Version");
            if (versionValue == null)
                return false;

            // This means we'll be false for preview versions but I'm not going down that rabbit hole anyway
            if (Version.TryParse(versionValue.ToString(), out Version dotnetVersion))
                return dotnetVersion.Major >= 5;
            return false;
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
