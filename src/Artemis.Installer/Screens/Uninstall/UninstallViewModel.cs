using System.Collections.Generic;
using System.Linq;
using Artemis.Installer.Screens.Abstract;
using MaterialDesignExtensions.Controllers;
using MaterialDesignExtensions.Controls;
using Stylet;

namespace Artemis.Installer.Screens.Uninstall
{
    public class UninstallViewModel : Conductor<UninstallStepViewModel>.Collection.OneActive
    {
        private StepperController _stepperController;

        public UninstallViewModel(IEnumerable<UninstallStepViewModel> configurationSteps)
        {
            Items.AddRange(configurationSteps.OrderBy(s => s.Order));
        }

        public void ActiveStepChanged(object sender, ActiveStepChangedEventArgs e)
        {
            Stepper stepper = (Stepper) sender;
            _stepperController = stepper.Controller;

            int activeStepIndex = stepper.Steps.IndexOf(e.Step);
            if (Items.Count > activeStepIndex)
                ActiveItem = Items[activeStepIndex];
            else
                _stepperController.Back();
        }

        public void Cancel()
        {
            RequestClose();
        }
    }
}