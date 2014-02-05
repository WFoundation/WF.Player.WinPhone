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

			// Adds a blocking content presenter to block and show progress.
			AddBlockingContentPresenter();

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

		protected override void OnReady()
		{
			// Sets the initial selected index.
			ContentPivot.SelectedIndex = ViewModel.PivotSelectedIndex;

			// Sets the filters for the task collection views.
			ViewModel.InitCollectionViewSourcesForTasks(_CurrentTasksSource, _HistoryTasksSource);

            // Informs the data model that the page is ready.
            ViewModel.OnPageReady();
		}

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
	}
}