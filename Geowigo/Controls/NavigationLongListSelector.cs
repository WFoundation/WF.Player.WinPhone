using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Geowigo.Controls
{
    public class NavigationLongListSelector : LongListSelector
    {
        #region Dependency Properties

		#region NavigationCommand


		public ICommand NavigationCommand
		{
			get { return (ICommand)GetValue(NavigationCommandProperty); }
			set { SetValue(NavigationCommandProperty, value); }
		}

		// Using a DependencyProperty as the backing store for NavigationCommand.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty NavigationCommandProperty =
			DependencyProperty.Register("NavigationCommand", typeof(ICommand), typeof(NavigationLongListSelector), new PropertyMetadata(null));


		#endregion
		
		#endregion
		
        public NavigationLongListSelector()
		{
			// Defines event handlers.
			SelectionChanged += new SelectionChangedEventHandler(OnSelectionChanged);
		}

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Avoid entering an infinite loop.
			if (e.AddedItems.Count == 0 || e.AddedItems[0] == null)
			{
				return;
			}

            LongListSelector lb = (LongListSelector)sender;

			// Navigates to the details of the first selected item.
			if (NavigationCommand != null)
			{
                // Gets the first selected item.
                object target = e.AddedItems.OfType<object>().FirstOrDefault();
				
                // Executes the command if possible.
                if (NavigationCommand.CanExecute(target))
				{
					NavigationCommand.Execute(target);
				} 
			}

			// Clears the listbox selection.
			((LongListSelector)sender).SelectedItem = null;
		}
    }
}
