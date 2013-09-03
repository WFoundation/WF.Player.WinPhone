using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Geowigo.ViewModels;
using WF.Player.Core;

namespace Geowigo.Views
{
	public partial class GameHomePage : BasePage
	{
		#region Properties

		public new GameHomeViewModel ViewModel
		{
			get
			{
				return (GameHomeViewModel)base.ViewModel;
			}
			set
			{
				base.ViewModel = value;
			}
		}

		#endregion
		
		public GameHomePage()
		{
			InitializeComponent();

			RegisterListBoxHelpers(this.InventoryList);
			RegisterListBoxHelpers(this.ObjectList);
			RegisterListBoxHelpers(this.ZoneList);
			RegisterListBoxHelpers(this.TaskList);
		}

		protected override void OnListBoxSelectionChangedOverride(ListBox lb, SelectionChangedEventArgs e)
		{
			if (lb == InventoryList || lb == ObjectList || lb == ZoneList || lb == TaskList)
			{
				// Gets the first selected item to navigate to.
				UIObject thing = e.AddedItems.OfType<UIObject>().FirstOrDefault();

				// Navigates to the details.
				ViewModel.ShowDetailsCommand.Execute(thing);
			}
		}

		private void ContentPivot_Loaded(object sender, RoutedEventArgs e)
		{
			// Sets the initial selected index.
			((Pivot)sender).SelectedIndex = ViewModel.PivotSelectedIndex;
		}
	}
}