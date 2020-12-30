using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Artemis.Installer.Extensions;
using Artemis.Installer.Services.Prerequisites;

namespace Artemis.Installer.Services
{
    public class InstallationService : IInstallationService
    {
        public InstallationService(IEnumerable<IPrerequisite> prerequisites)
        {
            InstallationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Artemis");
            Prerequisites = prerequisites.ToList();
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

        public string InstallationDirectory { get; set; }
        public List<IPrerequisite> Prerequisites { get; }
        public List<string> Args { get; set; }
        public bool IsUnattended => Args != null && Args.Contains("-unattended");
    }
}