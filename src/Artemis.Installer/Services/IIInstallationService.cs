using System.Collections.Generic;
using System.Threading.Tasks;
using Artemis.Installer.Services.Prerequisites;
using Artemis.Installer.Utilities;
using Microsoft.Win32;

namespace Artemis.Installer.Services
{
    public interface IInstallationService
    {
        string InstallationDirectory { get; set; }
        List<IPrerequisite> Prerequisites { get; }
        List<string> Args { get; set; }
        bool IsUnattended { get; }
        Task<string> DownloadPrerequisite(IPrerequisite prerequisite);
        Task InstallPrerequisite(IPrerequisite prerequisite, string file);
        Task<string> GetBinariesVersion(string branch = "master");
        Task<string> DownloadBinaries(string version, IDownloadable downloadable, string branch = "master");
        Task InstallBinaries(string file, IDownloadable downloadable);
        RegistryKey GetInstallKey();
        void CreateInstallKey(string version, string branch = "master");
    }
}