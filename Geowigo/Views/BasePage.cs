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
using Microsoft.Phone.Controls;
using Geowigo.ViewModels;

namespace Geowigo.Views
{
	/// <summary>
	/// A base class for pages that display Wherigo content using a BaseViewModel.
	/// </summary>
	public abstract class BasePage : PhoneApplicationPage
	{
		#region Properties

		/// <summary>
		/// Gets or sets the ViewModel used by this page.
		/// </summary>
		public BaseViewModel ViewModel 
		{
			get
			{
				return this.DataContext as BaseViewModel;
			}

			set
			{
				this.DataContext = value;
			}
		}

		#endregion

		#region PhoneApplicationPage Event Handling

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// Relays the event to the view model.
			ViewModel.OnPageNavigatedTo(e, NavigationContext);
		} 
		#endregion

		#region List box Helpers
		/// <summary>
		/// Registers event helpers for a ListBox.
		/// </summary>
		/// <param name="lb"></param>
		protected void RegisterListBoxHelpers(ListBox lb)
		{
			lb.SelectionChanged += new SelectionChangedEventHandler(OnListBoxSelectionChanged);
		}

		protected virtual void OnListBoxSelectionChangedOverride(ListBox lb, SelectionChangedEventArgs e)
		{

		}

		private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ListBox lb = (ListBox)sender;

			// Avoid entering an infinite loop
			if (e.AddedItems.Count == 0)
			{
				return;
			}

			// Children classes can perform their own operations.
			OnListBoxSelectionChangedOverride(lb, e);

			// Clears the listbox selection.
			((ListBox)sender).SelectedItem = null;
		} 
		#endregion


	}
}
