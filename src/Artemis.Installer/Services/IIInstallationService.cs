using System.Collections.Generic;
using Artemis.Installer.Services.Prerequisites;

namespace Artemis.Installer.Services
{
    public interface IInstallationService
    {
        string InstallationDirectory { get; set; }
        List<IPrerequisite> Prerequisites { get; }
    }
}