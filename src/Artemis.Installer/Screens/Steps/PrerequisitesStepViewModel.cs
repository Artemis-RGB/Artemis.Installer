using System.Linq;
using System.Threading.Tasks;
using Artemis.Installer.Screens.Steps.Prerequisites;
using Artemis.Installer.Services;
using Stylet;

namespace Artemis.Installer.Screens.Steps
{
    public class PrerequisitesStepViewModel : ConfigurationStep
    {
        private bool _canContinue;

        private bool _displayDownloadButton;
        private bool _displayProcess;

        private PrerequisiteViewModel _subject;

        public PrerequisitesStepViewModel(IInstallationService installationService)
        {
            Prerequisites = new BindableCollection<PrerequisiteViewModel>(installationService.Prerequisites.Select(p => new PrerequisiteViewModel(installationService, p)));
            foreach (PrerequisiteViewModel prerequisiteViewModel in Prerequisites)
                prerequisiteViewModel.ConductWith(this);
        }

        public BindableCollection<PrerequisiteViewModel> Prerequisites { get; }

        public bool CanContinue
        {
            get => _canContinue;
            set => SetAndNotify(ref _canContinue, value);
        }

        public PrerequisiteViewModel Subject
        {
            get => _subject;
            set => SetAndNotify(ref _subject, value);
        }

        public bool DisplayDownloadButton
        {
            get => _displayDownloadButton;
            set => SetAndNotify(ref _displayDownloadButton, value);
        }

        public bool DisplayProcess
        {
            get => _displayProcess;
            set => SetAndNotify(ref _displayProcess, value);
        }

        public override int Order => 2;

        public void Update()
        {
            foreach (PrerequisiteViewModel prerequisiteViewModel in Prerequisites)
                prerequisiteViewModel.Update();

            CanContinue = Prerequisites.All(p => p.IsMet);

            DisplayDownloadButton = Subject == null;
            DisplayProcess = Subject != null;
        }

        public async Task InstallMissing()
        {
            foreach (PrerequisiteViewModel prerequisiteViewModel in Prerequisites)
            {
                if (prerequisiteViewModel.IsMet)
                    continue;

                Subject = prerequisiteViewModel;
                Update();

                string file = await prerequisiteViewModel.Download();
                Update();
                await prerequisiteViewModel.Install(file);
            }

            Subject = null;
            Update();
        }

        protected override void OnActivate()
        {
            Update();
            base.OnActivate();
        }
    }
}