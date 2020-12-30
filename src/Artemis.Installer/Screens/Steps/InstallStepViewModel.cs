using System.Net.Http;
using System.Threading.Tasks;
using Artemis.Installer.Services;

namespace Artemis.Installer.Screens.Steps
{
    public class InstallStepViewModel : ConfigurationStep
    {
        private readonly IInstallationService _installationService;
        private bool _canContinue;
        private string _dadJoke;

        public InstallStepViewModel(IInstallationService installationService)
        {
            _installationService = installationService;
        }

        public override int Order => 4;

        public bool CanContinue
        {
            get => _canContinue;
            set => SetAndNotify(ref _canContinue, value);
        }

        public string DadJoke
        {
            get => _dadJoke;
            set => SetAndNotify(ref _dadJoke, value);
        }

        protected override void OnInitialActivate()
        {
            DadJoke = "Loading your dad joke...";
            base.OnInitialActivate();
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnActivate()
        {
            Task.Run(GetRandomFact);
            base.OnActivate();
        }

        private async Task GetRandomFact()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accept", "text/plain");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Artemis installer (https://github.com/Artemis-RGB/Artemis.Installer)");
                HttpResponseMessage result = await httpClient.GetAsync("https://icanhazdadjoke.com/");
                if (result.IsSuccessStatusCode)
                    DadJoke = (await result.Content.ReadAsStringAsync())?.Trim();
            }
        }

        #endregion
    }
}