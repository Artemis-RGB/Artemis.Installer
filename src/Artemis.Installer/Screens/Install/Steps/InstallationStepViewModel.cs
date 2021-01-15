using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Artemis.Installer.Screens.Abstract;
using Artemis.Installer.Services;
using Artemis.Installer.Utilities;
using MaterialDesignExtensions.Controls;
using Stylet;

namespace Artemis.Installer.Screens.Install.Steps
{
    public class InstallationStepViewModel : InstallStepViewModel, IDownloadable
    {
        private readonly IInstallationService _installationService;
        private bool _canContinue;
        private string _dadJoke = "Loading your dad joke...";
        private double _downloadCurrent;
        private double _downloadTotal;
        private bool _isDownloading;
        private float _processPercentage;
        private string _status;

        public InstallationStepViewModel(IInstallationService installationService)
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

        #region Implementation of IDownloadable

        /// <inheritdoc />
        public void ReportProgress(long currentBytes, long totalBytes, float percentage)
        {
            DownloadCurrent = currentBytes / 1024.0 / 1024.0;
            DownloadTotal = totalBytes / 1024.0 / 1024.0;
            ProcessPercentage = percentage;
        }

        #endregion

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            Execute.PostToUIThread(async () => await GetRandomFact());
            Execute.PostToUIThread(async () => await Install());
            base.OnInitialActivate();
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

            Status = "Closing down Artemis.";
            Process process = Process.GetProcessesByName("Artemis.UI").FirstOrDefault();
            // TODO: Do this gracefully, process.CloseMainWindow() won't do the trick because the tray has no handle
            if (process != null)
            {
                process.Kill();
                await Task.Delay(2000);
            }
            
            // Remove existing binaries
            Status = "Removing old files.";
            await _installationService.UninstallBinaries(this, true);
            
            // Extract the ZIP
            Status = "Extracting Artemis files.";
            await _installationService.InstallBinaries(file, this);

            // Create registry keys
            Status = "Finalizing installation.";
            _installationService.CreateInstallKey(version);

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
    }
}