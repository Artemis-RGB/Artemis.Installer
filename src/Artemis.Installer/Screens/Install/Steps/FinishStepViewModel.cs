using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Navigation;
using Artemis.Installer.Screens.Abstract;
using Artemis.Installer.Services;
using Artemis.Installer.Utilities;
using Stylet;

namespace Artemis.Installer.Screens.Install.Steps
{
    public class FinishStepViewModel : InstallStepViewModel
    {
        private readonly IInstallationService _installationService;
        private bool _createDesktopShortcut;
        private bool _startArtemis = true;

        public FinishStepViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public bool StartArtemis
        {
            get => _startArtemis;
            set => SetAndNotify(ref _startArtemis, value);
        }

        public bool CreateDesktopShortcut
        {
            get => _createDesktopShortcut;
            set => SetAndNotify(ref _createDesktopShortcut, value);
        }

        public override int Order => 5;

        public void OpenHyperlink(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(processInfo);
        }

        public void Finish()
        {
            if (CreateDesktopShortcut)
                _installationService.CreateDesktopShortcut();
            if (StartArtemis)
            {
                string executable = Path.Combine(_installationService.InstallationDirectory, "Artemis.UI.exe");
                ProcessUtilities.RunAsDesktopUser(executable);
            }

            ((Screen) Parent).RequestClose();
        }
    }
}