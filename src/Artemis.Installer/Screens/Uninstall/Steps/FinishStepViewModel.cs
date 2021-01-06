using System.Diagnostics;
using System.Windows.Navigation;
using Artemis.Installer.Screens.Abstract;
using Stylet;

namespace Artemis.Installer.Screens.Uninstall.Steps
{
    public class FinishStepViewModel : UninstallStepViewModel
    {
        public override int Order => 3;

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
            ((Screen) Parent).RequestClose();
        }
    }
}