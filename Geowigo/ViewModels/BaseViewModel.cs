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

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A base class for View models that deal with Wherigo objects.
	/// </summary>
	public class BaseViewModel : DependencyObject
	{
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
					OnModelChanging(_Model, value);

					_Model = value;

					OnModelChanged(_Model);
				}
			}
		}

		#endregion

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
		
		#region Fields
		private Models.WherigoModel _Model;
		#endregion

		#region Dependency Properties

		#region WherigoObject
		/// <summary>
		/// Gets the underlying WherigoObject that this ViewModel is bound to, if any.
		/// </summary>
		public Table WherigoObject
		{
			get { return (Table)GetValue(WherigoObjectProperty); }
			protected set { SetValue(WherigoObjectProperty, value); }
		}

		// Using a DependencyProperty as the backing store for WherigoObject.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty WherigoObjectProperty =
			DependencyProperty.Register("WherigoObject", typeof(Table), typeof(BaseViewModel), new PropertyMetadata(null, WherigoObjectProperty_PropertyChanged));

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

		#endregion

		public BaseViewModel()
		{
			// Base ressources construction.
			Model = App.Current.Model;
			Cartridge = Model.Core.Cartridge;
		}

		#region Navigation
		/// <summary>
		/// Called by pages when they are navigated to.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="navCtx"></param>
		public void OnPageNavigatedTo(NavigationEventArgs e, NavigationContext navCtx)
		{
			if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Refresh)
			{
				InitFromNavigation(navCtx);
			}
		}

		/// <summary>
		/// Initializes the view model from the navigation context.
		/// </summary>
		/// <remarks>The default behavior finds and binds to the right object depending
		/// on the Id field.</remarks>
		/// <param name="navCtx"></param>
		protected virtual void InitFromNavigation(NavigationContext navCtx)
		{
			// Parses the wherigo id parameter and tries to load its associed object.
			string rawWidParam;
			if (navCtx.QueryString.TryGetValue("wid", out rawWidParam))
			{
				int WidParam;
				if (int.TryParse(rawWidParam, out WidParam))
				{
					Table wObject;
					if (this.Model.Core.TryGetWherigoObject<Table>(WidParam, out wObject))
					{
						// The object has been found: keep it.
						this.WherigoObject = wObject;
					}
				}
			}

			// TODO: in case of nothing found, do something special ?
		} 
		#endregion

		#region Core' Properties Change
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

		private void OnModelChanging(Models.WherigoModel oldValue, Models.WherigoModel newValue)
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
		}

		private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			Engine es = (Engine)sender;
			
			// Discards if the engine is not ready.
			if (!es.IsReady)
			{
				return;
			}

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
		/// <param name="table"></param>
		protected virtual void OnWherigoObjectChanged(Table table)
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
			OnWherigoObjectChanged(e.NewValue as Table);
		}

		private void WherigoObject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{			
			// Redirects the event.
			OnWherigoObjectPropertyChanged(e.PropertyName);
		}

		#endregion
	}
}
