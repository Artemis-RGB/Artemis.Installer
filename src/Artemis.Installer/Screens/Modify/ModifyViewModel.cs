using System.Threading.Tasks;
using Artemis.Installer.Services;
using MaterialDesignExtensions.Controls;
using Stylet;

namespace Artemis.Installer.Screens.Modify
{
    public class ModifyViewModel : Screen
    {
        private readonly IInstallationService _installationService;

        private bool _installSelected;
        private bool _uninstallSelected;

        public ModifyViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public bool InstallSelected
        {
            get => _installSelected;
            set
            {
                SetAndNotify(ref _installSelected, value);
                NotifyOfPropertyChange(nameof(CanContinue));
            }
        }

        public bool UninstallSelected
        {
            get => _uninstallSelected;
            set
            {
                SetAndNotify(ref _uninstallSelected, value);
                NotifyOfPropertyChange(nameof(CanContinue));
            }
        }

        public bool CanContinue => InstallSelected || UninstallSelected;

        public async Task Continue()
        {
            if (InstallSelected && !await ShowWpfWarning())
                return;

            ((AttendedViewModel) Parent).ModifyChoiceSelected();
        }

        public void Cancel()
        {
            InstallSelected = false;
            UninstallSelected = false;

            ((AttendedViewModel) Parent).ModifyChoiceSelected();
        }

        private async Task<bool> ShowWpfWarning()
        {
            if (!_installationService.IsWpfVersionInstalled())
                return true;

            ConfirmationDialogArguments dialogArgs = new ConfirmationDialogArguments
            {
                Title = "Upgrading from WPF to Avalonia",
                Message = "It looks like you are upgrading to a major new Artemis update.\r\n" +
                          "THIS WILL WIPE ALL YOUR SETTINGS AND PROFILES.",
                OkButtonLabel = "OK, WIPE MY SETTINGS AND PROFILES",
                CancelButtonLabel = "CANCEL"
            };
            return await ConfirmationDialog.ShowDialogAsync("RootDialogHost", dialogArgs);
        }
    }
}