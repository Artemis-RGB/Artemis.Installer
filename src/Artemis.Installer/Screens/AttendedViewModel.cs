﻿using Artemis.Installer.Screens.Install;
using Artemis.Installer.Screens.Modify;
using Artemis.Installer.Screens.Uninstall;
using Artemis.Installer.Services;
using Stylet;

namespace Artemis.Installer.Screens
{
    public class AttendedViewModel : Conductor<Screen>
    {
        private readonly IInstallationService _installationService;
        private readonly InstallViewModel _installViewModel;
        private readonly ModifyViewModel _modifyViewModel;
        private readonly UninstallViewModel _uninstallViewModel;

        public AttendedViewModel(IInstallationService installationService, InstallViewModel installViewModel, ModifyViewModel modifyViewModel, UninstallViewModel uninstallViewModel)
        {
            _installationService = installationService;
            _installViewModel = installViewModel;
            _modifyViewModel = modifyViewModel;
            _uninstallViewModel = uninstallViewModel;
        }

        public void ModifyChoiceSelected()
        {
            if (_modifyViewModel.InstallSelected)
                ActiveItem = _installViewModel;
            else if (_modifyViewModel.UninstallSelected)
                ActiveItem = _uninstallViewModel;
            else
                RequestClose();

            ActiveItem.Closed += ActiveItemOnClosed;
        }

        protected override void OnInitialActivate()
        {
            if (_installationService.GetInstallKey() == null)
                ActiveItem = _installViewModel;
            else
                ActiveItem = _modifyViewModel;

            base.OnInitialActivate();
        }

        private void ActiveItemOnClosed(object sender, CloseEventArgs e)
        {
            ActiveItem.Closed -= ActiveItemOnClosed;
            RequestClose();
        }
    }
}