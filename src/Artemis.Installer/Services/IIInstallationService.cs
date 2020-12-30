using System.Collections.Generic;
using System.Threading.Tasks;
using Artemis.Installer.Services.Prerequisites;

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
    }
}