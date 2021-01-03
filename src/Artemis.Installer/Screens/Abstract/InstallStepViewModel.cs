using Stylet;

namespace Artemis.Installer.Screens.Abstract
{
    public abstract class InstallStepViewModel : OrderedScreen
    {
        /// <inheritdoc />
        protected InstallStepViewModel()
        {
        }

        /// <inheritdoc />
        protected InstallStepViewModel(IModelValidator validator) : base(validator)
        {
        }
    }
}