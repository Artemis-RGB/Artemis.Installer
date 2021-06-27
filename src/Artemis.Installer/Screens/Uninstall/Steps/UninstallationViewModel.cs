using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Artemis.Installer.Screens.Abstract;
using Artemis.Installer.Services;
using Artemis.Installer.Utilities;
using Stylet;

namespace Artemis.Installer.Screens.Uninstall.Steps
{
    public class UninstallationViewModel : UninstallStepViewModel, IDownloadable
    {
        private readonly IInstallationService _installationService;
        private bool _canContinue;
        private float _processPercentage;

        private string _status;

        public UninstallationViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public override int Order => 2;

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

        public float ProcessPercentage
        {
            get => _processPercentage;
            set => SetAndNotify(ref _processPercentage, value);
        }

        public async Task Uninstall()
        {
            Status = "Closing down Artemis in case it's running...";
            await _installationService.RemoteShutdown();

            Status = "Removing application files.";
            await _installationService.UninstallBinaries(this, false);
            Status = "Cleaning up registry.";
            _installationService.RemoveInstallKey();
            Status = "Removing shortcuts.";
            _installationService.RemoveDesktopShortcut();
            Status = "Uninstall finished.";

            CanContinue = true;
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            Execute.PostToUIThread(async () => await Uninstall());
            base.OnInitialActivate();
        }

        #endregion

        public void ReportProgress(long currentBytes, long totalBytes, float percentage)
        {
            ProcessPercentage = percentage;
        }
    }
}