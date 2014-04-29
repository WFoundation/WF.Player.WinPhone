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
		#region Fields

		private Geowigo.Controls.BlockingContentPresenter _blockingContentPresenter;

		#endregion
		
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

		#region Constructor

		public BasePage()
			: base()
		{
            Loaded += new RoutedEventHandler(BasePage_Loaded);
		}

		#endregion

		#region Blocking Content Presenter

		/// <summary>
		/// Adds a blocking content presenter to this page, that can
		/// block user interaction and display a progress bar and a message.
		/// </summary>
		/// <remarks>If this page already has a blocking content presenter,
		/// this method does nothing.</remarks>
		public void AddBlockingContentPresenter()
		{
			if (_blockingContentPresenter != null)
			{
				return;
			}

			// Creates the control.
			_blockingContentPresenter = new Controls.BlockingContentPresenter();
			_blockingContentPresenter.DataContext = ViewModel;
			
			// Sets the current content of this page as the inner content of the control.
			UIElement currentContent = Content;
			Content = null;
			_blockingContentPresenter.InnerContent = currentContent;
			
			// Sets the control as current content of this page.
			Content = _blockingContentPresenter;
		}

		#endregion

		#region PhoneApplicationPage Event Handling

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			base.OnBackKeyPress(e);

			// Relays the event to the view model.
			ViewModel.OnPageBackKeyPress(e);
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			System.Diagnostics.Debug.WriteLine("BasePage: Now on air - " + e.Uri.ToString());

			// Relays the event to the view model.
			ViewModel.OnPageNavigatedTo(e, NavigationContext);
		}

		protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			
			// Relays the event to the view model.
			ViewModel.OnPageNavigatingFrom(e, NavigationContext);
		}

		/// <summary>
		/// Called when the page is loaded and ready.
		/// </summary>
		protected virtual void OnReady()
		{

		}

		private void BasePage_LayoutUpdated(object sender, EventArgs e)
		{
            // Unregisters the event handler, because many more 
            // are coming our way.
            LayoutUpdated -= new EventHandler(BasePage_LayoutUpdated);

            // Raises OnReady in the dispatcher.
            // This way, we make sure the event will be fired at a moment
            // when the dispatcher is done rendering the page.
            // And, therefore, the page is probably ready.
            // (WTF, Silverlight?!)
            Dispatcher.BeginInvoke(OnReady);
		}

        void BasePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Unregisters the event handler, because more Loaded event may
            // come our way. (WTF, Silverlight?)
            Loaded -= new RoutedEventHandler(BasePage_Loaded);

            // Registers the handler for LayoutUpdated, because the next one
            // coming is a good candidate for the page nearing readiness.
            // (WTF, Silverlight?)
            LayoutUpdated += new EventHandler(BasePage_LayoutUpdated);
        }

		#endregion

	}
}
