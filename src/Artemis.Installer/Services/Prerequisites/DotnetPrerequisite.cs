﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
            // Unfortunately neither approach is foolproof, hopefully this will cover most cases.
            return IsFoundByDotnetCli() || IsFoundInRegistry();
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
        
        private bool IsFoundByDotnetCli()
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
                return Regex.IsMatch(versions, @"Microsoft\.WindowsDesktop\.App ([6-9].\d*.\d*).*");
            }
            catch (Win32Exception e)
            {
                // File not found
                if (e.NativeErrorCode == 2)
                    return false;
                throw;
            }
        }
        
        private bool IsFoundInRegistry()
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
    }
}