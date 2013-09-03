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

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A base class for View models that deal with Wherigo objects.
	/// </summary>
	public class BaseViewModel : DependencyObject
	{
		#region Properties

		/// <summary>
		/// Gets or sets the model used by this ViewModel.
		/// </summary>
		public Models.WherigoModel Model { get; internal set; }

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
			DependencyProperty.Register("WherigoObject", typeof(Table), typeof(BaseViewModel), new PropertyMetadata(null));

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
					if (this.Model.TryGetWherigoObject<Table>(WidParam, out wObject))
					{
						// The object has been found: keep it.
						this.WherigoObject = wObject;
					}
				}
			}

			// TODO: in case of nothing found, do something special ?
		}
		
	}
}
