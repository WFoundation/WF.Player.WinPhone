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
using WF.Player.Core;
using Geowigo.Controls;
using System.Windows.Navigation;
using WF.Player.Core.Engines;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using Windows.ApplicationModel.Activation;
using Geowigo.Utils;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A base class for View models that deal with Wherigo objects.
	/// </summary>
	public class BaseViewModel : DependencyObject
	{
		#region Nested Classes

		/// <summary>
		/// Provides information about a navigation operation.
		/// </summary>
		protected class NavigationInfo
		{
			/// <summary>
			/// Gets the navigation context.
			/// </summary>
			public NavigationContext NavigationContext { get; private set; }

			/// <summary>
			/// Gets the navigation mode.
			/// </summary>
			public NavigationMode NavigationMode { get; private set; }

            /// <summary>
            /// Gets the continuation operation, if this navigation event was triggered by a continuation
            /// activation event.
            /// </summary>
            public string ContinuationOperation { get; private set; }

            /// <summary>
            /// Gets the continuation event arguments, if this navigation event was triggered by a continuation
            /// activation event.
            /// </summary>
            public IContinuationActivatedEventArgs ContinuationEventArgs { get; private set; }

			public NavigationInfo(NavigationContext ctx, NavigationMode mode)
			{
				NavigationContext = ctx;
				NavigationMode = mode;

                // Checks for a continuation operation.
                IContinuationActivatedEventArgs e = App.Current.ViewModel.ContractContinuationActivatedEventArgs;
                ContinuationEventArgs = e;
                if (e != null)
                {
                    ContinuationOperation = e.ContinuationData.GetValueOrDefault<string>(ContinuationOperationKey);
                }
			}

			/// <summary>
			/// Gets the value corresponding to the NavigationContext's URI
			/// query string, or null if it was not found. 
			/// </summary>
			/// <param name="key"></param>
			/// <returns></returns>
			public string GetQueryValueOrDefault(string key)
			{
				string val = null;

				if (NavigationContext.QueryString.TryGetValue(key, out val))
				{
					return val;
				}

				return null;
			}
		}

		#endregion
		
		#region Properties

		#region Model
		/// <summary>
		/// Gets or sets the model used by this ViewModel.
		/// </summary>
		public Models.WherigoModel Model
		{
			get
			{
				return _Model;
			}

			internal set
			{
				if (_Model != value)
				{
					OnModelChangingInternal(_Model, value);

					_Model = value;

					OnModelChanged(_Model);
				}
			}
		}

		#endregion

		#region AppTitle
		/// <summary>
		/// Gets the application title.
		/// </summary>
		public string AppTitle
		{
			get
			{
				return App.Current.ViewModel.AppTitle;
			}
		} 
		#endregion

		#region IsPageVisible
		public bool IsPageVisible { get; private set; }
		#endregion

		#endregion

        #region Constants

        /// <summary>
        /// Key for operation data in continuation activated events' ContinuationData.
        /// </summary>
        public const string ContinuationOperationKey = "Operation";

        #endregion
		
		#region Fields

		private Models.WherigoModel _Model;

		private bool _hasMessageBoxOnScreen;

		private object _syncRoot = new object();
		#endregion

		#region Dependency Properties

		#region WherigoObject
		/// <summary>
		/// Gets the underlying WherigoObject that this ViewModel is bound to, if any.
		/// </summary>
        public WherigoObject WherigoObject
		{
            get { return (WherigoObject)GetValue(WherigoObjectProperty); }
			protected set { SetValue(WherigoObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for WherigoObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty WherigoObjectProperty =
            DependencyProperty.Register("WherigoObject", typeof(WherigoObject), typeof(BaseViewModel), new PropertyMetadata(null, WherigoObjectProperty_PropertyChanged));

		private static void WherigoObjectProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((BaseViewModel)o).OnWherigoObjectChangedInternal(e);
		}

		#endregion

		#region Cartridge
		/// <summary>
		/// Gets the current playing Cartridge.
		/// </summary>
		public Cartridge Cartridge
		{
			get { return (Cartridge)GetValue(CartridgeProperty); }
			protected set { SetValue(CartridgeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Cartridge.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CartridgeProperty =
			DependencyProperty.Register("Cartridge", typeof(Cartridge), typeof(BaseViewModel), new PropertyMetadata(null));

		#endregion

		#region ApplicationBar

		public IApplicationBar ApplicationBar
		{
			get { return (IApplicationBar)GetValue(ApplicationBarProperty); }
			set { SetValue(ApplicationBarProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ApplicationBar.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ApplicationBarProperty =
			DependencyProperty.Register("ApplicationBar", typeof(IApplicationBar), typeof(BaseViewModel), new PropertyMetadata(null, OnApplicationBarPropertyChanged));

		private static void OnApplicationBarPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((BaseViewModel)o).OnApplicationBarChanged(e.NewValue as IApplicationBar);
		}

		#endregion

		#region IsProgressBarVisible


		public bool IsProgressBarVisible
		{
			get { return (bool)GetValue(IsProgressBarVisibleProperty); }
			set { SetValue(IsProgressBarVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsProgressBarVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsProgressBarVisibleProperty =
			DependencyProperty.Register("IsProgressBarVisible", typeof(bool), typeof(BaseViewModel), new PropertyMetadata(false, OnIsProgressBarVisiblePropertyChanged));

		private static void OnIsProgressBarVisiblePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((BaseViewModel)o).OnIsProgressBarVisibleChanged((bool)e.NewValue);
		}

		#endregion

		#region ProgressBarStatusText


		public string ProgressBarStatusText
		{
			get { return (string)GetValue(ProgressBarStatusTextProperty); }
			set { SetValue(ProgressBarStatusTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ProgressBarStatusText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ProgressBarStatusTextProperty =
			DependencyProperty.Register("ProgressBarStatusText", typeof(string), typeof(BaseViewModel), new PropertyMetadata(null));


		#endregion

		#endregion

		public BaseViewModel()
		{
            if (DesignerProperties.IsInDesignTool)
            {
                // Prevents cast exceptions in editors.
                return;
            }
            
            // Base ressources construction.
			Model = App.Current.Model;
			Cartridge = Model.Core.Cartridge;

			// App view model event handlers.
			App.Current.ViewModel.MessageBoxManager.HasMessageBoxChanged += new EventHandler(OnAppViewModelHasMessageBoxChanged);
		}

		#region Navigation

		/// <summary>
		/// Called by pages when the back key is pressed.
		/// </summary>
		/// <param name="e"></param>
		public void OnPageBackKeyPress(CancelEventArgs e)
		{
			// Dismisses the current message box, if any.
			if (App.Current.ViewModel.MessageBoxManager.HasMessageBox)
			{
				e.Cancel = true;

				App.Current.ViewModel.MessageBoxManager.DismissAllMessageBoxes();
			}
			else
			{
				OnPageBackKeyPressOverride(e);
			}
		}

		/// <summary>
		/// Called when an allowed back key pressed event has occured.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnPageBackKeyPressOverride(CancelEventArgs e)
		{
			
		}

		/// <summary>
		/// Called by pages when they are navigated to.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="navCtx"></param>
		public void OnPageNavigatedTo(NavigationEventArgs e, NavigationContext navCtx)
		{
			// Marks the page as visible.
			IsPageVisible = true;

			// Always perform the common initializations.
			NavigationInfo nav = new NavigationInfo(navCtx, e.NavigationMode);
			InitFromNavigationInternal(nav);
			
			// This view model needs to be init'ed only if the navigation 
			// gets to the associated page for the first time, or if
			// the app is recovering from being tombstoned.
			if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Refresh)
			{
				InitFromNavigation(nav);
			}
			else if (e.NavigationMode == NavigationMode.Back)
			{
				// If we're back from tombstone or from a contract activation, initialize the page again.
                if (App.Current.ViewModel.HasRecoveredFromTombstone || nav.ContinuationEventArgs != null)
				{
					InitFromNavigation(nav);
				}
				else
				{
					OnPageNavigatedBackToOverride();
				}
			}
		}

		/// <summary>
		/// Called when the user navigated back to page.
		/// </summary>
		protected virtual void OnPageNavigatedBackToOverride()
		{
		}

		/// <summary>
		/// Initializes the view model from the navigation context.
		/// </summary>
		/// <param name="navCtx"></param>
		protected virtual void InitFromNavigation(NavigationInfo nav)
		{
		}

		private void InitFromNavigationInternal(NavigationInfo nav)
		{
			// Parses the wherigo id parameter and tries to load its associed object.
			string rawWidParam;
			if (nav.NavigationContext.QueryString.TryGetValue("wid", out rawWidParam))
			{
				int WidParam;
				if (int.TryParse(rawWidParam, out WidParam))
				{
					WherigoObject wObject;
					if (this.Model.Core.TryGetWherigoObject<WherigoObject>(WidParam, out wObject))
					{
						// The object has been found: keep it.
						this.WherigoObject = wObject;
					}
				}
			}
		}

		/// <summary>
		/// Called by the page when the navigation is leaving it.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="navctx"></param>
		public void OnPageNavigatingFrom(NavigatingCancelEventArgs e, NavigationContext navctx)
		{
			// Marks the page as invisible.
			IsPageVisible = false;
		}

		#endregion

		#region App View Model Event Handlers

		private void OnAppViewModelHasMessageBoxChanged(object sender, EventArgs e)
		{
			// Relays the information.
			OnHasMessageBoxChanged(((MessageBoxManager)sender).HasMessageBox);
		}

		/// <summary>
		/// Called when a non-native message box appears or disappears. This allows
		/// view models to adjust their layout, such as removing application bars.
		/// </summary>
		/// <remarks>The default implementation hides the application bar when a
		/// message box appears and shows it when a message box disappears.</remarks>
		/// <param name="hasMessageBox"></param>
		protected virtual void OnHasMessageBoxChanged(bool hasMessageBox)
		{
			// Remembers this value.
			lock (_syncRoot)
			{
				_hasMessageBoxOnScreen = hasMessageBox;
			}

			// Hides or shows the application bar if there is any.
			if (ApplicationBar != null)
			{
				RefreshApplicationBarIsVisible(hasMessageBox: hasMessageBox);
			}
		}

		#endregion

		#region This View Model Properties Change

		private void RefreshApplicationBarIsVisible(bool? hasMessageBox = null, bool? isProgressBarVisible = null)
		{
			// Gets if there are message boxes.
			if (!hasMessageBox.HasValue)
			{
				lock (_syncRoot)
				{
					hasMessageBox = _hasMessageBoxOnScreen;
				}
			}

			// Gets if the progress bar is visible.
			if (!isProgressBarVisible.HasValue)
			{
				isProgressBarVisible = IsProgressBarVisible;
			}

			// Applies the visiblity.
			ApplicationBar.IsVisible = !hasMessageBox.Value && !isProgressBarVisible.Value;
		}

		private void OnApplicationBarChanged(IApplicationBar bar)
		{
			if (bar != null)
			{
				// Hides or shows the app
				RefreshApplicationBarIsVisible();
			}
		}

		private void OnIsProgressBarVisibleChanged(bool newValue)
		{
			// Hides or shows the app bar if there is no message box and the
			// progress bar is hidden.
			if (ApplicationBar != null)
			{
				RefreshApplicationBarIsVisible(isProgressBarVisible: newValue);
			}
		}

		#endregion

		#region Core' Properties Change
		
		/// <summary>
		/// Called when the state of the game engine has changed.
		/// </summary>
		/// <param name="oldState">State the engine had before the change occured.</param>
		/// <param name="newState">State the engine has now.</param>
		protected virtual void OnCoreGameStateChanged(EngineGameState oldState, EngineGameState newState)
		{

		} 
		
		/// <summary>
		/// Called when a property of the Wherigo Core has changed.
		/// </summary>
		/// <param name="propName">Property that has changed.</param>
		protected virtual void OnCorePropertyChanged(string propName)
		{

		}

		/// <summary>
		/// Called when the underlying model has changed.
		/// </summary>
		/// <param name="newModel"></param>
		protected virtual void OnModelChanged(Models.WherigoModel newModel)
		{

		}

		/// <summary>
		/// Called when the underlying model is changing.
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		protected virtual void OnModelChanging(Models.WherigoModel oldValue, Models.WherigoModel newValue)
		{
			
		}

		private void OnModelChangingInternal(Models.WherigoModel oldValue, Models.WherigoModel newValue)
		{
			// Unregisters old event handlers.
			if (oldValue != null)
			{
				oldValue.Core.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
			}

			// Registers new event handlers.
			if (newValue != null)
			{
				newValue.Core.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
			}

			// Notifies children.
			OnModelChanging(oldValue, newValue);
		}

		private void Core_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Engine es = (Engine)sender;
			
			// Relays the event if it contains important information about the
			// engine's state.
			if (e.PropertyName == "GameState")
			{
				PropertyChangedExtendedEventArgs<EngineGameState> eExtended = (PropertyChangedExtendedEventArgs<EngineGameState>)e;

				OnCoreGameStateChanged(eExtended.OldValue, eExtended.NewValue);
			}

			// Discards if the engine is not ready.
			if (!es.IsReady)
			{
				return;
			}

			// Relays the event.
			OnCorePropertyChanged(e.PropertyName);
		}
		#endregion

		#region WherigoObject' Properties Change

		/// <summary>
		/// Called when a property of the associated Wherigo object has changed.
		/// </summary>
		/// <param name="propName"></param>
		protected virtual void OnWherigoObjectPropertyChanged(string propName)
		{

		}
		
		/// <summary>
		/// Called when the associated Wherigo object has changed.
		/// </summary>
		/// <param name="obj"></param>
        protected virtual void OnWherigoObjectChanged(WherigoObject obj)
		{
			
		}
		
		private void OnWherigoObjectChangedInternal(DependencyPropertyChangedEventArgs e)
		{
			// Unregisters old event handlers.
			UIObject oldUio = e.OldValue as UIObject;
			if (oldUio != null)
			{
				oldUio.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(WherigoObject_PropertyChanged);
			}

			// Registers new event handlers.
			UIObject newUio = e.NewValue as UIObject;
			if (newUio != null)
			{
				newUio.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(WherigoObject_PropertyChanged);
			}

			// Propagates the event.
            OnWherigoObjectChanged(e.NewValue as WherigoObject);
		}

		private void WherigoObject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{			
			// Redirects the event.
			OnWherigoObjectPropertyChanged(e.PropertyName);
		}

		#endregion
	}
}
