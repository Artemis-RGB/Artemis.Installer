﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Artemis.Installer.Services.Prerequisites;
using Artemis.Installer.Utilities;
using Microsoft.Win32;

namespace Artemis.Installer.Services
{
    public interface IInstallationService
    {
        string InstallationDirectory { get; set; }
        string DataDirectory { get; }
        List<IPrerequisite> Prerequisites { get; }
        List<string> Args { get; set; }
        bool RemoveAppData { get; set; }
        bool CleanUpOnShutdown { get; set; }
        
        Task<string> DownloadPrerequisite(IPrerequisite prerequisite);
        Task InstallPrerequisite(IPrerequisite prerequisite, string file);

        Task<string> GetBinariesVersion(string branch = "refs/heads/master");
        Task<string> DownloadBinaries(string version, IDownloadable downloadable, string branch = "refs/heads/master");
        Task InstallBinaries(string file, IDownloadable downloadable);
        Task UninstallBinaries(IDownloadable downloadable, bool onlyDelete);
        Task RemoteShutdown();

        RegistryKey GetInstallKey();
        void CreateInstallKey(string version, string branch = "refs/heads/master");
        void RemoveInstallKey();
        void CreateDesktopShortcut();
        void RemoveDesktopShortcut();
    }
}