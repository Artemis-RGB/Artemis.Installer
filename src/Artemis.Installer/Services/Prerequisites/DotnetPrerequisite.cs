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
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost");
            object versionValue = key?.GetValue("Version");
            if (versionValue == null)
                return false;

            // Splitting on '-' because of semver values like 6.0.0-rc.1.21451.13 which Version.TryParse can't handle
            if (Version.TryParse(versionValue.ToString().Split('-')[0], out Version dotnetVersion))
                return dotnetVersion.Major >= 6;
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
