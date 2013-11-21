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
using Geowigo.Models;
using Geowigo.Controls;
using WF.Player.Core;

namespace Geowigo.ViewModels
{
	public class HomeViewModel
	{
		#region Properties

		#region AppTitle

		public string AppTitle
		{
			get
			{
				return App.Current.ViewModel.AppTitle;
			}
		}

		#endregion

		#region Model

		public WherigoModel Model { get; private set; }

		#endregion

		#endregion

		#region Commands

		#region StartCartridgeCommand

		private ICommand _StartCartridgeCommand;

		public ICommand StartCartridgeCommand
		{
			get
			{
				if (_StartCartridgeCommand == null)
				{
					_StartCartridgeCommand = new RelayCommand<CartridgeTag>(StartCartridge);
				} 

				return _StartCartridgeCommand;
			}
		}

		#endregion

		#region ShowCartridgeInfoCommand

		private ICommand _ShowCartridgeInfoCommand;

		public ICommand ShowCartridgeInfoCommand
		{
			get
			{
				if (_ShowCartridgeInfoCommand == null)
				{
					_ShowCartridgeInfoCommand = new RelayCommand<CartridgeTag>(ShowCartridgeInfo);
				}

				return _ShowCartridgeInfoCommand;
			}
		}

		#endregion

		#endregion

		public HomeViewModel()
		{
			Model = App.Current.Model;
		}

		public void InitFromNavigation(System.Windows.Navigation.NavigationEventArgs e)
		{
			// Removes the back entries, if any.
			App.Current.ViewModel.ClearBackStack();
			
			// Synchronizes the cartridge store.
			Model.CartridgeStore.SyncFromIsoStore();
		}

		private void StartCartridge(CartridgeTag cartTag)
		{
			// Starts the cartridge!
			App.Current.ViewModel.NavigateToGameHome(cartTag.Cartridge.Filename);
		}

		private void ShowCartridgeInfo(CartridgeTag cartTag)
		{
			// Show the cartridge info!
			App.Current.ViewModel.NavigateToCartridgeInfo(cartTag);
		}
	}
}
