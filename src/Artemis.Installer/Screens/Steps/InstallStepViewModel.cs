using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Artemis.Installer.Services;
using Artemis.Installer.Utilities;
using MaterialDesignExtensions.Controls;
using Stylet;

namespace Artemis.Installer.Screens.Steps
{
    public class InstallStepViewModel : ConfigurationStep, IDownloadable
    {
        private readonly IInstallationService _installationService;
        private bool _canContinue;
        private string _dadJoke = "Loading your dad joke...";
        private double _downloadCurrent;
        private double _downloadTotal;
        private float _processPercentage;
        private string _status;
        private bool _isDownloading;

        public InstallStepViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public override int Order => 4;

        public string Status
        {
            get => _status;
            set => SetAndNotify(ref _status, value);
        }

        public bool CanContinue
        {
            get => _canContinue;
            set => SetAndNotify(ref _canContinue, value);
        }

        public string DadJoke
        {
            get => _dadJoke;
            set => SetAndNotify(ref _dadJoke, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetAndNotify(ref _isDownloading, value);
        }

        public double DownloadCurrent
        {
            get => _downloadCurrent;
            set => SetAndNotify(ref _downloadCurrent, value);
        }

        public double DownloadTotal
        {
            get => _downloadTotal;
            set => SetAndNotify(ref _downloadTotal, value);
        }

        public float ProcessPercentage
        {
            get => _processPercentage;
            set => SetAndNotify(ref _processPercentage, value);
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnActivate()
        {
            Execute.PostToUIThread(async () => await GetRandomFact());
            Execute.PostToUIThread(async () => await Install());
            base.OnActivate();
        }

        private async Task Install()
        {
            Status = "Retrieving latest Artemis build number.";
            string version = await _installationService.GetBinariesVersion();

            if (version == null)
            {
                AlertDialogArguments dialogArgs = new AlertDialogArguments
                {
                    Title = "No binaries found",
                    Message = "We couldn't find a valid Artemis download, setup cannot continue.",
                    OkButtonLabel = "CLOSE :("
                };

                await AlertDialog.ShowDialogAsync("RootDialogHost", dialogArgs);
                Application.Current.Shutdown(1);
            }

            // Download the file
            Status = null;
            IsDownloading = true;
            string file = await _installationService.DownloadBinaries(version, this);
            IsDownloading = false;

            // Extract the ZIP
            Status = "Extracting Artemis files.";
            await _installationService.InstallBinaries(file, this);
            
            // Create registry keys
            Status = "Finalizing installation.";
            _installationService.CreateInstallKey(version);
            // Copy ourselves to the install dir
            File.Copy(System.Reflection.Assembly.GetCallingAssembly().Location, Path.Combine(_installationService.InstallationDirectory, "Artemis.Installer.exe"), true);
            // Remove the install archive
            File.Delete(file);
            
            Status = "Installation finished.";
            CanContinue = true;
        }

        private async Task GetRandomFact()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accept", "text/plain");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Artemis installer (https://github.com/Artemis-RGB/Artemis.Installer)");
                HttpResponseMessage result = await httpClient.GetAsync("https://icanhazdadjoke.com/");
                if (result.IsSuccessStatusCode)
                    DadJoke = (await result.Content.ReadAsStringAsync())?.Trim();
            }
        }

        #endregion

        #region Implementation of IDownloadable

        /// <inheritdoc />
        public void ReportProgress(long currentBytes, long totalBytes, float percentage)
        {
            DownloadCurrent = (currentBytes / 1024.0) / 1024.0;
            DownloadTotal = (totalBytes / 1024.0) / 1024.0;
            ProcessPercentage = percentage;
        }

        #endregion
    }
}