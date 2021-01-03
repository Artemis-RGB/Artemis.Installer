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

        private string _status;
        private bool _canContinue;
        private float _processPercentage;
        
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

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnActivate()
        {
            Execute.PostToUIThread(async () => await Uninstall());
            base.OnActivate();
        }

        #endregion

        public async Task Uninstall()
        {
            await _installationService.UninstallBinaries(this);
        }

        public void ReportProgress(long currentBytes, long totalBytes, float percentage)
        {
            ProcessPercentage = percentage;
        }

    }
}