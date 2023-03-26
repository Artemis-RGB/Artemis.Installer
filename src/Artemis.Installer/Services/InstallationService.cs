using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Artemis.Installer.Extensions;
using Artemis.Installer.Utilities;
using Microsoft.Win32;

namespace Artemis.Installer.Services
{
    public class InstallationService : IInstallationService
    {
        private const string API_URL = "https://updating.artemis-rgb.com";
        private readonly string _artemisStartMenuDirectory;

        public InstallationService()
        {
            RegistryKey installKey = GetInstallKey();
            InstallationDirectory = installKey != null
                ? installKey.GetValue("InstallLocation").ToString()
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Artemis");
            DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Artemis");

            _artemisStartMenuDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs\Artemis"
            );
        }

        private void CreateDirectoryForFile(string path)
        {
            GeneralUtilities.CreateAccessibleDirectory(Path.GetDirectoryName(path));
        }

        private async Task DeleteAppData(IDownloadable downloadable)
        {
            // Get all the files recursively as our total
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Artemis");
            if (!Directory.Exists(directory))
            {
                downloadable.ReportProgress(0, 0, 100);
                return;
            }

            string source = Assembly.GetEntryAssembly()?.Location;
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            // Delete all files
            await Task.Run(() =>
            {
                int index = 0;
                foreach (string file in files)
                {
                    if (file != source)
                        File.Delete(file);

                    index++;
                    downloadable.ReportProgress(index, files.Length, index / (float) files.Length * 100);
                }
            });

            downloadable.ReportProgress(0, 0, 100);
        }

        private void KillThemAll()
        {
            Process[] processes = Process.GetProcessesByName("Artemis.UI.Windows");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // ignored I guess
                }
            }
        }

        public async Task<string> DownloadBinaries(IDownloadable downloadable, string branch)
        {
            string file = Path.GetTempFileName();
            File.Move(file, file.Replace(".tmp", ".zip"));
            file = file.Replace(".tmp", ".zip");

            // Escape the branch
            branch = Uri.EscapeDataString(branch);
            using (HttpClient httpClient = new HttpClient())
            {
                string hash = await httpClient.GetStringAsync($"{API_URL}/api/artifacts/latest/{branch}/windows/hash");

                // Download archive
                using (FileStream fileStream = new FileStream(file, FileMode.Open))
                {
                    try
                    {
                        await httpClient.DownloadAsync($"{API_URL}/api/artifacts/latest/{branch}/windows", fileStream, downloadable);
                    }
                    catch (HttpRequestException e)
                    {
                        if (e.InnerException is WebException webException)
                            throw new Exception($"Failed to download binaries, please ensure you have a working internet connection.\r\n{webException.Status}", e);
                        throw new Exception("Failed to download binaries, please ensure you have a working internet connection.", e);
                    }

                    // Validate SHA256 hash
                    string computedHash;
                    fileStream.Seek(0, SeekOrigin.Begin);
                    using (MD5 md5 = MD5.Create())
                    {
                        computedHash = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", string.Empty);
                    }

                    if (!hash.Equals(computedHash))
                        throw new Exception("Download hash mismatch, this means the downloaded files are corrupt, please try again.");
                }
            }

            return file;
        }

        public async Task InstallBinaries(string file, IDownloadable downloadable)
        {
            CleanUpOnShutdown = false;

            GeneralUtilities.CreateAccessibleDirectory(DataDirectory);
            GeneralUtilities.CreateAccessibleDirectory(InstallationDirectory);
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                ZipArchive archive = new ZipArchive(fileStream);
                float count = 0;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    using (Stream unzippedEntryStream = entry.Open())
                    {
                        downloadable.ReportProgress(0, 0, count / archive.Entries.Count * 100f);
                        if (entry.Length > 0)
                        {
                            string path = Path.Combine(InstallationDirectory, entry.FullName);
                            CreateDirectoryForFile(path);
                            using (Stream extractStream = new FileStream(path, FileMode.OpenOrCreate))
                            {
                                await unzippedEntryStream.CopyToAsync(extractStream);
                            }
                        }
                    }

                    count++;
                }
            }

            downloadable.ReportProgress(0, 0, 100);

            // Copy installer
            string source = Assembly.GetEntryAssembly().Location;
            string target = Path.Combine(DataDirectory, "installer", "Artemis.Installer.exe");
            if (source != target)
            {
                CreateDirectoryForFile(target);
                File.Copy(source, target, true);
            }

            // Populate the start menu
            if (!Directory.Exists(_artemisStartMenuDirectory))
                Directory.CreateDirectory(_artemisStartMenuDirectory);

            ShortcutUtilities.Create(
                Path.Combine(_artemisStartMenuDirectory, "Artemis.lnk"),
                Path.Combine(InstallationDirectory, "Artemis.UI.Windows.exe"),
                "",
                InstallationDirectory,
                "Artemis",
                "",
                ""
            );
            ShortcutUtilities.Create(
                Path.Combine(_artemisStartMenuDirectory, "Uninstall Artemis.lnk"),
                Path.Combine(DataDirectory, "installer", "Artemis.Installer.exe"),
                "-uninstall",
                InstallationDirectory,
                "Uninstall Artemis",
                "",
                ""
            );
        }

        public async Task UninstallBinaries(IDownloadable downloadable, bool onlyDelete)
        {
            string source = Assembly.GetEntryAssembly().Location;

            if (Directory.Exists(InstallationDirectory))
            {
                // Get all the files recursively as our total
                string[] files = Directory.GetFiles(InstallationDirectory, "*", SearchOption.AllDirectories);
                // Delete all files except the installer
                await Task.Run(() =>
                {
                    int index = 0;
                    foreach (string file in files)
                    {
                        if (file != source)
                            File.Delete(file);

                        index++;
                        downloadable.ReportProgress(index, files.Length, index / (float) files.Length * 100);
                    }
                });
            }

            downloadable.ReportProgress(0, 0, 100);

            if (onlyDelete)
                return;

            // Delete the installer itself after it closes
            CleanUpOnShutdown = true;

            // If needed, repeat for app data
            if (RemoveAppData)
                await DeleteAppData(downloadable);

            // Clean up the start menu
            if (Directory.Exists(_artemisStartMenuDirectory))
                Directory.Delete(_artemisStartMenuDirectory, true);
        }

        public RegistryKey GetInstallKey()
        {
            return Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", true);
        }

        public void CreateInstallKey()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", true) ??
                              Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", true);

            key.SetValue("DisplayIcon", Path.Combine(InstallationDirectory, "Artemis.UI.Windows.exe"), RegistryValueKind.String);
            key.SetValue("DisplayName", "Artemis 2", RegistryValueKind.String);
            key.SetValue("HelpLink", "https://wiki.artemis-rgb.com", RegistryValueKind.String);
            key.SetValue("InstallLocation", InstallationDirectory, RegistryValueKind.String);
            key.SetValue("Publisher", "Artemis RGB", RegistryValueKind.String);
            key.SetValue("UninstallString", $"\"{Path.Combine(DataDirectory, "installer", "Artemis.Installer.exe")}\" -uninstall", RegistryValueKind.String);
            key.SetValue("ModifyPath", $"\"{Path.Combine(DataDirectory, "installer", "Artemis.Installer.exe")}\"", RegistryValueKind.String);
            key.SetValue("URLInfoAbout", "https://artemis-rgb.com", RegistryValueKind.String);

            key.Close();
        }

        /// <inheritdoc />
        public void RemoveInstallKey()
        {
            Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", false);
        }

        /// <inheritdoc />
        public void CreateDesktopShortcut()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Artemis.lnk");
            ShortcutUtilities.Create(path, Path.Combine(InstallationDirectory, "Artemis.UI.Windows.exe"), "", InstallationDirectory, "Artemis", "", "");
        }

        /// <inheritdoc />
        public void RemoveDesktopShortcut()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Artemis.lnk");
            if (File.Exists(path))
                File.Delete(path);
        }

        public bool IsWpfVersionInstalled()
        {
            RegistryKey installKey = GetInstallKey();
            if (installKey == null)
                return false;

            object displayVersionValue = installKey.GetValue("DisplayVersion");
            if (displayVersionValue == null)
                return false;
            string version = installKey.GetValue("DisplayVersion").ToString();
            if (!int.TryParse(version.Split('.')[0], out int major))
                return false;

            return major <= 20220715;
        }

        public async Task RemoteShutdown()
        {
            try
            {
                // It is unlikely Artemis is already running in this case, lets just check though
                if (!File.Exists(Path.Combine(DataDirectory, "webserver.txt")))
                    return;

                string url = File.ReadAllText(Path.Combine(DataDirectory, "webserver.txt"));
                using (HttpClient client = new HttpClient())
                {
                    await client.PostAsync(url + "remote/shutdown", null);
                    await Task.Delay(2000);
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                KillThemAll();
                await Task.Delay(1000);
            }
        }

        public List<string> Args { get; set; }
        public string InstallationDirectory { get; set; }
        public string DataDirectory { get; }
        public bool RemoveAppData { get; set; }
        public bool CleanUpOnShutdown { get; set; }
    }
}