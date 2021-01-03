using Stylet;

namespace Artemis.Installer.Screens.Abstract
{
    public abstract class OrderedScreen : Screen
    {
        /// <inheritdoc />
        protected OrderedScreen()
        {
        }

        /// <inheritdoc />
        protected OrderedScreen(IModelValidator validator) : base(validator)
        {
        }

        public abstract int Order { get; }
    }
}