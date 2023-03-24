using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Artemis.Installer.Services;
using Artemis.Installer.Utilities;
using MaterialDesignExtensions.Controls;
using Stylet;

namespace Artemis.Installer.Screens
{
    public class AutoUpdateViewModel : Screen, IDownloadable
    {
        private readonly IInstallationService _installationService;
        private bool _canCancel;
        private bool _cancelled;
        private double _downloadCurrent;
        private double _downloadTotal;
        private string _installStatus;
        private bool _isDownloading;

        private float _processPercentage;
        private string _status;
        private float _statusPercentage;

        public AutoUpdateViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public bool CanCancel
        {
            get => _canCancel;
            set => SetAndNotify(ref _canCancel, value);
        }

        public float ProcessPercentage
        {
            get => _processPercentage;
            set => SetAndNotify(ref _processPercentage, value);
        }

        public float StatusPercentage
        {
            get => _statusPercentage;
            set => SetAndNotify(ref _statusPercentage, value);
        }

        public string Status
        {
            get => _status;
            set => SetAndNotify(ref _status, value);
        }

        public string InstallStatus
        {
            get => _installStatus;
            set => SetAndNotify(ref _installStatus, value);
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

        public void Cancel()
        {
            _cancelled = true;
            CanCancel = false;
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            Execute.PostToUIThread(async () => await AutoUpdate());
            base.OnInitialActivate();
        }

        #endregion

        private async Task AutoUpdate()
        {
            float steps = 2;
            float step = 0;

            await ShowWpfWarning();
            if (Cancelled())
                return;

            CanCancel = true;

            Status = "Closing down Artemis in case it's running...";
            await _installationService.RemoteShutdown();

            if (Cancelled())
                return;

            step++;
            ProcessPercentage = step / steps * 100f;
            Status = "Downloading & installing latest Artemis build.";

            if (Cancelled())
                return;

            // Download the file
            InstallStatus = null;
            IsDownloading = true;
            string file = await _installationService.DownloadBinaries(this);

            IsDownloading = false;

            if (Cancelled())
            {
                File.Delete(file);
                return;
            }

            CanCancel = false;

            // Remove existing binaries
            InstallStatus = "Removing old files.";
            await _installationService.UninstallBinaries(this, true);

            // Extract the ZIP
            InstallStatus = "Extracting Artemis files.";
            await _installationService.InstallBinaries(file, this);

            // Create registry keys
            InstallStatus = "Finalizing installation.";
            _installationService.CreateInstallKey();

            // Remove the install archive
            File.Delete(file);

            step++;
            ProcessPercentage = step / steps * 100f;
            InstallStatus = "Installation finished.";

            await Task.Delay(TimeSpan.FromSeconds(1));
            string executable = Path.Combine(_installationService.InstallationDirectory, "Artemis.UI.Windows.exe");
            ProcessUtilities.RunAsDesktopUser(executable);

            // RequestClose also closes Artemis..?
            // This does not happen in FinishStepViewModel and I can't tell why.
            Application.Current.Shutdown(1);
        }

        private async Task ShowWpfWarning()
        {
            if (!_installationService.IsWpfVersionInstalled())
                return;

            ConfirmationDialogArguments dialogArgs = new ConfirmationDialogArguments
            {
                Title = "Upgrading from WPF to Avalonia",
                Message = "It looks like you are upgrading to a major new Artemis update.\r\n" +
                          "THIS WILL WIPE ALL YOUR SETTINGS AND PROFILES.",
                OkButtonLabel = "OK, WIPE MY SETTINGS AND PROFILES",
                CancelButtonLabel = "CANCEL"
            };
            _cancelled = !await ConfirmationDialog.ShowDialogAsync("RootDialogHost", dialogArgs);
        }

        private bool Cancelled()
        {
            // By the time the user clicks CLOSE, the caller should have cleaned up
            if (_cancelled)
                Execute.PostToUIThreadAsync(async () =>
                {
                    AlertDialogArguments dialogArgs = new AlertDialogArguments
                    {
                        Title = "Auto-update cancelled",
                        Message = "Any pre-requisites already installed will have to be uninstalled manually.",
                        OkButtonLabel = "CLOSE"
                    };

                    await AlertDialog.ShowDialogAsync("RootDialogHost", dialogArgs);
                    Application.Current.Shutdown(1);
                });

            return _cancelled;
        }

        #region Implementation of IDownloadable

        /// <inheritdoc />
        public void ReportProgress(long currentBytes, long totalBytes, float percentage)
        {
            DownloadCurrent = currentBytes / 1024.0 / 1024.0;
            DownloadTotal = totalBytes / 1024.0 / 1024.0;
            StatusPercentage = percentage;
        }

        #endregion
    }
}