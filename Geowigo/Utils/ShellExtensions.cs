using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Shell;

namespace Geowigo.Utils
{
    public static class ShellExtensions
    {
        /// <summary>
        /// Creates a button and adds it to an application bar.
        /// </summary>
        /// <param name="appBar"></param>
        /// <param name="iconFilenameRelative"></param>
        /// <param name="command"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ApplicationBarIconButton CreateAndAddButton(this IApplicationBar appBar, string iconFilenameRelative, ICommand command, string text)
        {
            ApplicationBarIconButton btn = new ApplicationBarIconButton(new Uri("/icons/" + iconFilenameRelative, UriKind.Relative));

            // First-time values.
            btn.IsEnabled = command.CanExecute(btn);
            btn.Text = text;

            // Adds click handler to execute the command upon click.
            btn.Click += (o, e) =>
            {
                if (command.CanExecute(btn))
                {
                    command.Execute(btn);
                }
            };

            // Adds CanExecute changed handler.
            command.CanExecuteChanged += (o, e) =>
            {
                btn.IsEnabled = command.CanExecute(btn);
            };

            // Adds the button.
            appBar.Buttons.Add(btn);

            return btn;
        }

        /// <summary>
        /// Creates a menu item and adds it to an application bar.
        /// </summary>
        /// <param name="appBar"></param>
        /// <param name="command"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ApplicationBarMenuItem CreateAndAddMenuItem(this IApplicationBar appBar, ICommand command, string text)
        {
            ApplicationBarMenuItem mi = new ApplicationBarMenuItem(text);

            // First-time values.
            mi.IsEnabled = command.CanExecute(mi);

            // Adds click handler to execute the command upon click.
            mi.Click += (o, e) =>
            {
                if (command.CanExecute(mi))
                {
                    command.Execute(mi);
                }
            };

            // Adds CanExecute changed handler.
            command.CanExecuteChanged += (o, e) =>
            {
                mi.IsEnabled = command.CanExecute(mi);
            };

            // Adds the button.
            appBar.MenuItems.Add(mi);

            return mi;
        }
    }
}
