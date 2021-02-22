using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Artemis.Installer.Screens.Install.Prerequisites;
using Artemis.Installer.Services;
using Artemis.Installer.Utilities;
using MaterialDesignExtensions.Controls;
using Stylet;

namespace Artemis.Installer.Screens
{
    public class AutoUpdateViewModel : Screen, IDownloadable
    {
        private readonly IInstallationService _installationService;
        private PrerequisiteViewModel _activePrerequisite;
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

        public PrerequisiteViewModel ActivePrerequisite
        {
            get => _activePrerequisite;
            set
            {
                SetAndNotify(ref _activePrerequisite, value);
                NotifyOfPropertyChange(nameof(IsPrerequisiteActive));
            }
        }

        public bool IsPrerequisiteActive => ActivePrerequisite != null;

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

            CanCancel = true;

            Status = "Closing down Artemis.";
            Process process = Process.GetProcessesByName("Artemis.UI").FirstOrDefault();
            // TODO: Do this gracefully, process.CloseMainWindow() won't do the trick because the tray has no handle
            if (process != null)
            {
                process.Kill();
                await Task.Delay(2);
            }

            if (Cancelled())
                return;

            Status = "Downloading & installing missing prerequisites.";
            // Get all prerequisites that aren't met
            List<PrerequisiteViewModel> prerequisites = _installationService.Prerequisites
                .Where(p => !p.IsMet())
                .Select(p => new PrerequisiteViewModel(_installationService, p))
                .ToList();

            steps += prerequisites.Count;
            foreach (PrerequisiteViewModel prerequisiteViewModel in prerequisites)
            {
                ActivePrerequisite = prerequisiteViewModel;
                string prerequisiteFile = await prerequisiteViewModel.Download();

                if (Cancelled())
                {
                    File.Delete(prerequisiteFile);
                    return;
                }

                await prerequisiteViewModel.Install(prerequisiteFile);
                if (Cancelled())
                    return;

                step++;
                ProcessPercentage = step / steps * 100f;
            }

            step++;
            ProcessPercentage = step / steps * 100f;
            Status = "Downloading & installing latest Artemis build.";

            InstallStatus = "Retrieving latest Artemis build number.";
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

            if (Cancelled())
                return;

            // Download the file
            InstallStatus = null;
            IsDownloading = true;
            string file = await _installationService.DownloadBinaries(version, this);

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
            _installationService.CreateInstallKey(version);

            // Remove the install archive
            File.Delete(file);

            step++;
            ProcessPercentage = step / steps * 100f;
            InstallStatus = "Installation finished.";

            await Task.Delay(TimeSpan.FromSeconds(1));
            string executable = Path.Combine(_installationService.InstallationDirectory, "Artemis.UI.exe");
            ProcessUtilities.RunAsDesktopUser(executable);

            // RequestClose also closes Artemis..?
            // This does not happen in FinishStepViewModel and I can't tell why.
            Application.Current.Shutdown(1);
        }

        private bool Cancelled()
        {
            if (_cancelled)
                // By the time the user clicks CLOSE, the caller should have cleaned up
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