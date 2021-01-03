using Stylet;

namespace Artemis.Installer.Screens.Abstract
{
    public abstract class UninstallStepViewModel : OrderedScreen
    {
        /// <inheritdoc />
        protected UninstallStepViewModel()
        {
        }

        /// <inheritdoc />
        protected UninstallStepViewModel(IModelValidator validator) : base(validator)
        {
        }
    }
}