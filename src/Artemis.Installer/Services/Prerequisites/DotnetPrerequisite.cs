﻿using System;
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

        public string DownloadUrl =>
            "https://download.visualstudio.microsoft.com/download/pr/c6a74d6b-576c-4ab0-bf55-d46d45610730/" +
            "f70d2252c9f452c2eb679b8041846466/windowsdesktop-runtime-5.0.1-win-x64.exe";

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