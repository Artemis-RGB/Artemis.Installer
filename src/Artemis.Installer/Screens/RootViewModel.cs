using System.Diagnostics;
using System.IO;
using System.Reflection;
using Artemis.Installer.Services;
using Stylet;

namespace Artemis.Installer.Screens
{
    public class RootViewModel : Conductor<Screen>
    {
        private readonly AttendedViewModel _attendedViewModel;
        private readonly IInstallationService _installationService;
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
            ActiveItem.Closed -= ActiveItemOnClosed;
            if (_installationService.RemoveInstallationDirectoryOnShutdown)
            {
                Process.Start(new ProcessStartInfo
                {
                    Arguments = $"/C PING -n 2 127.0.0.1>nul & RMDIR /Q /S \"{_installationService.InstallationDirectory}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                });
            }

            RequestClose();
        }
    }
}