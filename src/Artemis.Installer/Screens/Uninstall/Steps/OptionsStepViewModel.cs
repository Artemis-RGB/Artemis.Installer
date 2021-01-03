using Artemis.Installer.Screens.Abstract;
using Artemis.Installer.Services;

namespace Artemis.Installer.Screens.Uninstall.Steps
{
    public class OptionsStepViewModel : UninstallStepViewModel
    {
        private readonly IInstallationService _installationService;
        private string _installationDirectory;
        private bool _keepAppData;
        private bool _removeAppData;

        public OptionsStepViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public string InstallationDirectory
        {
            get => _installationDirectory;
            set => SetAndNotify(ref _installationDirectory, value);
        }

        public bool RemoveAppData
        {
            get => _removeAppData;
            set
            {
                SetAndNotify(ref _removeAppData, value);
                NotifyOfPropertyChange(nameof(CanContinue));
            }
        }

        public bool KeepAppData
        {
            get => _keepAppData;
            set
            {
                SetAndNotify(ref _keepAppData, value);
                NotifyOfPropertyChange(nameof(CanContinue));
            }
        }

        public bool CanContinue => RemoveAppData || KeepAppData;

        public override int Order => 1;

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnActivate()
        {
            InstallationDirectory = _installationService.InstallationDirectory;
            base.OnActivate();
        }

        /// <inheritdoc />
        protected override void OnDeactivate()
        {
            _installationService.RemoveAppData = RemoveAppData;
            base.OnDeactivate();
        }

        #endregion
    }
}