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

	}
}
