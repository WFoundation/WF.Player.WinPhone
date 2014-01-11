using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;

namespace Geowigo.Controls
{
	/// <summary>
	/// A list box that provides navigation facilities.
	/// </summary>
	public class NavigationListBox : ListBox
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
			DependencyProperty.Register("NavigationCommand", typeof(ICommand), typeof(NavigationListBox), new PropertyMetadata(null));


		#endregion
		
		#endregion
		
		#region Fields
		
		private bool _isManipulating = false; 
		
		#endregion

		public NavigationListBox()
		{
			// Defines event handlers.
			SelectionChanged += new SelectionChangedEventHandler(OnSelectionChanged);
		}

		protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
		{
			_isManipulating = true;

			base.OnManipulationStarted(e);
		}

		protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
		{			
			base.OnManipulationCompleted(e);

			_isManipulating = false;
		}

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Avoid entering an infinite loop
			if (e.AddedItems.Count == 0)
			{
				return;
			}
			
			ListBox lb = (ListBox)sender;

			// Navigates to the details of the first selected item.
			if (NavigationCommand != null && _isManipulating)
			{
                // Considers that the manipulation is over, so that
                // if the selected item changes during the execution of the
                // navigation command, the command does not run again.
                _isManipulating = false;
                
                // Gets the first selected item.
                object target = e.AddedItems.OfType<object>().FirstOrDefault();
				
                // Executes the command if possible.
                if (NavigationCommand.CanExecute(target))
				{
					NavigationCommand.Execute(target);
				} 
			}

            // The navigation cannot possibly be still happening.
            _isManipulating = false;

			// Clears the listbox selection.
			((ListBox)sender).SelectedItem = null;
		}

	}
}
