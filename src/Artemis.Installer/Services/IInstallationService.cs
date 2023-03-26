using System.Collections.Generic;
using System.Threading.Tasks;
using Artemis.Installer.Utilities;
using Microsoft.Win32;

namespace Artemis.Installer.Services
{
    public interface IInstallationService
    {
        string InstallationDirectory { get; set; }
        string DataDirectory { get; }
        List<string> Args { get; set; }
        bool RemoveAppData { get; set; }
        bool CleanUpOnShutdown { get; set; }

        Task<string> DownloadBinaries(IDownloadable downloadable, string branch = "master");
        Task InstallBinaries(string file, IDownloadable downloadable);
        Task UninstallBinaries(IDownloadable downloadable, bool onlyDelete);
        Task RemoteShutdown();

        RegistryKey GetInstallKey();
        void CreateInstallKey();
        void RemoveInstallKey();
        void CreateDesktopShortcut();
        void RemoveDesktopShortcut();
        bool IsWpfVersionInstalled();
    }
}