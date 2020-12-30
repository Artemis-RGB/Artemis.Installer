namespace Artemis.Installer.Utilities
{
    public interface IDownloadable
    {
        void ReportProgress(long currentBytes, long totalBytes, float percentage);
    }
}