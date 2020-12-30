using System;
using System.Threading.Tasks;
using Artemis.Installer.Services;
using Artemis.Installer.Services.Prerequisites;
using Stylet;

namespace Artemis.Installer.Screens.Steps.Prerequisites
{
    public class PrerequisiteViewModel : Screen
    {
        private readonly IInstallationService _installationService;
        private string _description;
        private bool _isMet;
        private string _title;
        private double _downloadCurrent;
        private double _downloadTotal;
        private double _downloadPercentage;
        private bool _isInstalling;

        public PrerequisiteViewModel(IInstallationService installationService, IPrerequisite prerequisite)
        {
            _installationService = installationService;
            Prerequisite = prerequisite;
        }

        public IPrerequisite Prerequisite { get; }

        public string Title
        {
            get => _title;
            set => SetAndNotify(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetAndNotify(ref _description, value);
        }

        public bool IsMet
        {
            get => _isMet;
            set => SetAndNotify(ref _isMet, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            set => SetAndNotify(ref _isInstalling, value);
        }

        public double DownloadCurrent
        {
            get => _downloadCurrent;
            private set => SetAndNotify(ref _downloadCurrent, value);
        }

        public double DownloadTotal
        {
            get => _downloadTotal;
            private set => SetAndNotify(ref _downloadTotal, value);
        }

        public double DownloadPercentage
        {
            get => _downloadPercentage;
            private set => SetAndNotify(ref _downloadPercentage, value);
        }

        public void Update()
        {
            Title = Prerequisite.Title;
            Description = Prerequisite.Description;
            IsMet = Prerequisite.IsMet();
        }

        private void PrerequisiteOnDownloadProgressUpdated(object sender, EventArgs e)
        {
            DownloadCurrent = (Prerequisite.DownloadCurrentBytes / 1024.0) / 1024.0;
            DownloadTotal = (Prerequisite.DownloadTotalBytes / 1024.0) / 1024.0;
            DownloadPercentage = Prerequisite.DownloadPercentage;
        }

        #region Overrides of Screen

        /// <inheritdoc />
        protected override void OnInitialActivate()
        {
            Prerequisite.DownloadProgressUpdated += PrerequisiteOnDownloadProgressUpdated;
            base.OnInitialActivate();
        }

        /// <inheritdoc />
        protected override void OnClose()
        {
            Prerequisite.DownloadProgressUpdated -= PrerequisiteOnDownloadProgressUpdated;
            base.OnClose();
        }

        #endregion

        public async Task<string> Download()
        {
            return await _installationService.DownloadPrerequisite(Prerequisite);
        }

        public async Task Install(string file)
        {
            IsInstalling = true;
            await _installationService.InstallPrerequisite(Prerequisite, file);
            IsInstalling = false;
        }
    }
}