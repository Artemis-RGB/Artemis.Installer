using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Artemis.Installer.Extensions;
using Artemis.Installer.Services.Prerequisites;
using Artemis.Installer.Utilities;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace Artemis.Installer.Services
{
    public class InstallationService : IInstallationService
    {
        private string _artemisStartMenuDirectory;

        public InstallationService(IEnumerable<IPrerequisite> prerequisites)
        {
            RegistryKey installKey = GetInstallKey();
            InstallationDirectory = installKey != null
                ? installKey.GetValue("InstallLocation").ToString()
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Artemis");

            Prerequisites = prerequisites.ToList();

            _artemisStartMenuDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs\Artemis"
            );
        }

        public async Task<string> DownloadPrerequisite(IPrerequisite prerequisite)
        {
            string file = Path.GetTempFileName();
            File.Move(file, file.Replace(".tmp", ".exe"));
            file = file.Replace(".tmp", ".exe");

            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    prerequisite.IsDownloading = true;
                    await httpClient.DownloadAsync(prerequisite.DownloadUrl, fileStream, prerequisite);
                    prerequisite.IsDownloading = false;
                    return file;
                }
            }
        }

        public async Task InstallPrerequisite(IPrerequisite prerequisite, string file)
        {
            prerequisite.IsInstalling = true;
            await prerequisite.Install(file);
            File.Delete(file);
            prerequisite.IsInstalling = false;
        }

        public async Task<string> GetBinariesVersion(string branch)
        {
            // Directory list
            IConfiguration config = Configuration.Default.WithDefaultLoader();
            IBrowsingContext context = BrowsingContext.New(config);
            IDocument document = await context.OpenAsync($"https://builds.artemis-rgb.com/binaries/{branch}/");

            Regex hrefRegex = new Regex("^\\d*.\\d*\\/");

            IHtmlAnchorElement anchor = document.All.Where(e => e is IHtmlAnchorElement)
                .Cast<IHtmlAnchorElement>()
                .Where(a => hrefRegex.IsMatch(a.InnerHtml))
                .OrderByDescending(a => a.InnerHtml).FirstOrDefault();
            return anchor?.InnerHtml?.TrimEnd('/');
        }

        public async Task<string> DownloadBinaries(string version, IDownloadable downloadable, string branch)
        {
            string file = Path.GetTempFileName();
            File.Move(file, file.Replace(".tmp", ".zip"));
            file = file.Replace(".tmp", ".zip");

            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    await httpClient.DownloadAsync($"https://builds.artemis-rgb.com/binaries/{branch}/{version}/artemis-build.zip", fileStream, downloadable);
                    return file;
                }
            }
        }

        public async Task InstallBinaries(string file, IDownloadable downloadable)
        {
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
                            if (!Directory.Exists(Path.GetDirectoryName(path)))
                            {
                                DirectorySecurity ds = new DirectorySecurity();
                                ds.AddAccessRule(new FileSystemAccessRule(
                                    new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                                    FileSystemRights.ReadAndExecute,
                                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                    PropagationFlags.None,
                                    AccessControlType.Allow)
                                );
                                ds.AddAccessRule(new FileSystemAccessRule(
                                    new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                                    FileSystemRights.FullControl,
                                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                    PropagationFlags.None,
                                    AccessControlType.Allow)
                                );

                                Directory.CreateDirectory(Path.GetDirectoryName(path), ds);
                            }

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

            // Populate the start menu
            if (!Directory.Exists(_artemisStartMenuDirectory))
                Directory.CreateDirectory(_artemisStartMenuDirectory);

            ShortcutUtilities.Create(
                Path.Combine(_artemisStartMenuDirectory, "Artemis.lnk"),
                Path.Combine(InstallationDirectory, "Artemis.UI.exe"),
                "",
                InstallationDirectory,
                "Artemis",
                "",
                ""
            );
            ShortcutUtilities.Create(
                Path.Combine(_artemisStartMenuDirectory, "Uninstall Artemis.lnk"),
                Path.Combine(InstallationDirectory, "Artemis.Installer.exe"),
                "-uninstall",
                InstallationDirectory,
                "Uninstall Artemis",
                "",
                ""
            );
        }

        public async Task UninstallBinaries(IDownloadable downloadable)
        {
            // Get all the files recursively as our total
            string[] files = Directory.GetFiles(InstallationDirectory, "*", SearchOption.AllDirectories);

            // Delete all files

            // Delete the folder itself

            // If needed, repeat for app data
            if (RemoveAppData)
                await DeleteAppData(downloadable);
        }

        private async Task DeleteAppData(IDownloadable downloadable)
        {
            // Get all the files recursively as our total
            string[] files = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Artemis"), "*", SearchOption.AllDirectories);

            // Delete all files

            // Delete the folder itself
        }

        public RegistryKey GetInstallKey()
        {
            return Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", true);
        }

        public void CreateInstallKey(string version, string branch)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", true) ??
                              Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Artemis 2", true);

            key.SetValue("DisplayIcon", Path.Combine(InstallationDirectory, "Artemis.UI.exe"), RegistryValueKind.String);
            key.SetValue("DisplayName", "Artemis 2", RegistryValueKind.String);
            key.SetValue("DisplayVersion", version, RegistryValueKind.String);
            key.SetValue("HelpLink", "https://wiki.artemis-rgb.com", RegistryValueKind.String);
            key.SetValue("InstallLocation", InstallationDirectory, RegistryValueKind.String);
            key.SetValue("Publisher", "Artemis RGB", RegistryValueKind.String);
            key.SetValue("UninstallString", $"\"{Path.Combine(InstallationDirectory, "Artemis.Installer.exe")}\" -uninstall", RegistryValueKind.String);
            key.SetValue("URLInfoAbout", "https://artemis-rgb.com", RegistryValueKind.String);
            key.SetValue("Branch", branch, RegistryValueKind.String);

            key.Close();
        }

        /// <inheritdoc />
        public void RemoveInstallKey()
        {
            throw new NotImplementedException();
        }

        public List<string> Args { get; set; }
        public List<IPrerequisite> Prerequisites { get; }
        public string InstallationDirectory { get; set; }
        public bool RemoveAppData { get; set; }

        public bool IsUnattended => Args != null && Args.Contains("-unattended");
    }
}