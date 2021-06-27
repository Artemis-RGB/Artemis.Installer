using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Ookii.Dialogs.Wpf;

namespace Artemis.Installer
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ShowException(e.Exception);
            e.Handled = true;
        }

        private void ShowException(Exception exception)
        {
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = "Artemis installer";
                dialog.MainInstruction = "Unfortunately the installer ran into an unhandled exception and cannot continue.";
                dialog.Content = exception.Message;
                dialog.ExpandedInformation = exception.ToStringDemystified();

                dialog.CollapsedControlText = "Show stack trace";
                dialog.ExpandedControlText = "Hide stack trace";

                dialog.Footer = "If this keeps happening check out the <a href=\"https://wiki.artemis-rgb.com\">wiki</a> or hit us up on <a href=\"https://discord.gg/S3MVaC9\">Discord</a>.";
                dialog.FooterIcon = TaskDialogIcon.Error;
                dialog.EnableHyperlinks = true;
                dialog.HyperlinkClicked += OpenHyperlink;

                TaskDialogButton copyButton = new TaskDialogButton("Copy stack trace");
                TaskDialogButton closeButton = new TaskDialogButton("Close") {Default = true};
                dialog.Buttons.Add(copyButton);
                dialog.Buttons.Add(closeButton);
                dialog.ButtonClicked += (sender, args) =>
                {
                    if (args.Item == copyButton)
                    {
                        Clipboard.SetText(exception.ToStringDemystified());
                        args.Cancel = true;
                    }
                };

                dialog.ShowDialog(Current.MainWindow);
            }
            Current.Shutdown(1);
        }

        public void OpenHyperlink(object sender, HyperlinkClickedEventArgs e)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = e.Href,
                UseShellExecute = true
            };
            Process.Start(processInfo);
        }
    }
}