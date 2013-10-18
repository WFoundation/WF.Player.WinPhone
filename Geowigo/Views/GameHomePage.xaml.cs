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
using System.Windows.Data;

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

		#region Fields

		CollectionViewSource _HistoryTasksSource;
		CollectionViewSource _CurrentTasksSource;

		#endregion
		
		public GameHomePage()
		{		
			InitializeComponent();

			// Gets resources.
			_HistoryTasksSource = (CollectionViewSource)this.Resources["HistoryTasksSource"];
			_CurrentTasksSource = (CollectionViewSource)this.Resources["CurrentTasksSource"];
		}

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			base.OnBackKeyPress(e);

			// Relays the event to the view model.
			ViewModel.OnBackKeyPress(e);
		}

		private void ContentPivot_Loaded(object sender, RoutedEventArgs e)
		{
			// Sets the initial selected index.
			((Pivot)sender).SelectedIndex = ViewModel.PivotSelectedIndex;
		}

		private void TasksPivotItem_Loaded(object sender, RoutedEventArgs e)
		{
			// Sets the filters for the task collection views.
			ViewModel.InitCollectionViewSourcesForTasks(_CurrentTasksSource, _HistoryTasksSource);
		}
	}
}