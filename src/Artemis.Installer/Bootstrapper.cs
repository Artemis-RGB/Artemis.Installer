using System.Linq;
using Artemis.Installer.Screens;
using Artemis.Installer.Screens.Abstract;
using Artemis.Installer.Services;
using Artemis.Installer.Services.Prerequisites;
using Artemis.Installer.Stylet;
using FluentValidation;
using Stylet;
using StyletIoC;

namespace Artemis.Installer
{
    public class Bootstrapper : Bootstrapper<RootViewModel>
    {
        #region Overrides of Bootstrapper<RootViewModel>

        /// <inheritdoc />
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            // View related stuff
            builder.Bind<InstallStepViewModel>().ToAllImplementations();
            builder.Bind<UninstallStepViewModel>().ToAllImplementations();
            builder.Bind(typeof(IPrerequisite)).ToAllImplementations();

            // Services
            builder.Bind<IInstallationService>().To<InstallationService>().InSingletonScope();

            // Validation
            builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentValidationAdapter<>));
            builder.Bind(typeof(IValidator<>)).ToAllImplementations();

            base.ConfigureIoC(builder);
        }

        #region Overrides of BootstrapperBase

        /// <inheritdoc />
        protected override void Configure()
        {
            Container.Get<IInstallationService>().Args = Args.ToList();
            base.Configure();
        }

        #endregion

        #endregion
    }
}