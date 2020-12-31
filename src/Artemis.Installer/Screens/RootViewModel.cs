using Artemis.Installer.Services;
using Stylet;

namespace Artemis.Installer.Screens
{
    public class RootViewModel : Conductor<Screen>
    {
        private readonly IInstallationService _installationService;
        private readonly AttendedViewModel _attendedViewModel;
        private readonly UnattendedViewModel _unattendedViewModel;

        public RootViewModel(IInstallationService installationService, AttendedViewModel attendedViewModel, UnattendedViewModel unattendedViewModel)
        {
            _installationService = installationService;
            _attendedViewModel = attendedViewModel;
            _unattendedViewModel = unattendedViewModel;
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            if (_installationService.IsUnattended)
                ActiveItem = _unattendedViewModel;
            else
                ActiveItem = _attendedViewModel;

            ActiveItem.Closed += ActiveItemOnClosed;
            base.OnInitialActivate();
        }

        #endregion

        private void ActiveItemOnClosed(object sender, CloseEventArgs e)
        {
            RequestClose();
        }
    }
}