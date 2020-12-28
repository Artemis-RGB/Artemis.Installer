using Artemis.Installer.Services;

namespace Artemis.Installer.Screens.Steps
{
    public class WelcomeStepViewModel : ConfigurationStep
    {
        private readonly IInstallationService _installationService;

        public WelcomeStepViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public override int Order => 1;
    }
}