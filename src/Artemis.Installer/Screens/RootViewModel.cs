using System.Diagnostics;
using System.IO;
using Artemis.Installer.Services;
using Stylet;

namespace Artemis.Installer.Screens
{
    public class RootViewModel : Conductor<Screen>
    {
        private readonly AttendedViewModel _attendedViewModel;
        private readonly AutoUpdateViewModel _autoUpdateViewModel;
        private readonly IInstallationService _installationService;

        public RootViewModel(IInstallationService installationService, AttendedViewModel attendedViewModel, AutoUpdateViewModel autoUpdateViewModel)
        {
            _installationService = installationService;
            _attendedViewModel = attendedViewModel;
            _autoUpdateViewModel = autoUpdateViewModel;
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            if (_installationService.Args.Contains("-autoupdate"))
                ActiveItem = _autoUpdateViewModel;
            else
                ActiveItem = _attendedViewModel;

            ActiveItem.Closed += ActiveItemOnClosed;
            base.OnInitialActivate();
        }

        #endregion

        private void ActiveItemOnClosed(object sender, CloseEventArgs e)
        {
            ActiveItem.Closed -= ActiveItemOnClosed;
            if (_installationService.CleanUpOnShutdown)
            {
                string path = _installationService.DataDirectory;
                if (!_installationService.RemoveAppData)
                    path = Path.Combine(_installationService.DataDirectory, "installer");

                Process.Start(new ProcessStartInfo
                {
                    Arguments = $"/C PING -n 2 127.0.0.1>nul & RMDIR /Q /S \"{path}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                });

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