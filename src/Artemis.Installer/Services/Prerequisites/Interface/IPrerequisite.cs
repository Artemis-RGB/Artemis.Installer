using System;
using System.Threading.Tasks;
using Artemis.Installer.Utilities;

namespace Artemis.Installer.Services.Prerequisites
{
    public interface IPrerequisite : IDownloadable
    {
        string Title { get; }
        string Description { get; }
        string DownloadUrl { get; }

        bool IsDownloading { get; set; }
        bool IsInstalling { get; set; }

        long DownloadCurrentBytes { get; }
        long DownloadTotalBytes { get; }
        float DownloadPercentage { get; }

        bool IsMet();
        Task Install(string file);

        event EventHandler DownloadProgressUpdated;
    }
}