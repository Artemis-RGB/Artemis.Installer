using System;
using System.Threading.Tasks;
using Artemis.Installer.Utilities;
using Microsoft.Win32;

namespace Artemis.Installer.Services.Prerequisites
{
    public class RedistPrerequisite : IPrerequisite
    {
        protected virtual void OnDownloadProgressUpdated()
        {
            DownloadProgressUpdated?.Invoke(this, EventArgs.Empty);
        }

        public string Title => "Visual C++ Redistributable for VS 2015, 2017 and 2019 x64";
        public string Description => "The C++ Redistributable is required for many device SDKs, the download is about 15 MB";
        public string DownloadUrl => "https://aka.ms/vs/16/release/vc_redist.x64.exe";

        public bool IsDownloading { get; set; }
        public bool IsInstalling { get; set; }

        public long DownloadCurrentBytes { get; private set; }
        public long DownloadTotalBytes { get; private set; }
        public float DownloadPercentage { get; private set; }

        public bool IsMet()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64", false);
            object majorValue = key?.GetValue("Major");
            if (majorValue == null)
                return false;

            return int.Parse(majorValue.ToString()) >= 14;
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