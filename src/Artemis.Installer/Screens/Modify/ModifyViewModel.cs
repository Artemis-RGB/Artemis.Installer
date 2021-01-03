using Stylet;

namespace Artemis.Installer.Screens.Modify
{
    public class ModifyViewModel : Screen
    {
        private bool _installSelected;
        private bool _uninstallSelected;

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

        public void Continue()
        {
            ((AttendedViewModel) Parent).ModifyChoiceSelected();
        }

        public void Cancel()
        {
            InstallSelected = false;
            UninstallSelected = false;

            ((AttendedViewModel) Parent).ModifyChoiceSelected();
        }
    }
}