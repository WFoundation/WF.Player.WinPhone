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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Geowigo.Controls;
using Microsoft.Phone.Controls;
using System.Windows.Data;

namespace Geowigo.ViewModels
{
	public class GameHomeViewModel : BaseViewModel
	{
		public static readonly string CartridgeFilenameKey = "cartFilename";
		public static readonly string SectionKey = "section";
		public static readonly string SectionValue_Overview = "overview";
		public static readonly string SectionValue_World = "world";
		public static readonly string SectionValue_Inventory = "inventory";
		public static readonly string SectionValue_Tasks = "tasks";

		#region Dependency Properties

		#region PivotSelectedIndex



		public int PivotSelectedIndex
		{
			get { return (int)GetValue(PivotSelectedIndexProperty); }
			set { SetValue(PivotSelectedIndexProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PivotSelectedIndex.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PivotSelectedIndexProperty =
			DependencyProperty.Register("PivotSelectedIndex", typeof(int), typeof(GameHomeViewModel), new PropertyMetadata(0));



		#endregion

		#endregion

		#region Commands

		#region ShowDetailsCommand

		private ICommand _ShowDetailsCommand;

		/// <summary>
		/// Gets a command to show the details of a thing.
		/// </summary>
		public ICommand ShowDetailsCommand
		{
			get
			{
				return _ShowDetailsCommand ?? (_ShowDetailsCommand = new RelayCommand<UIObject>(ShowDetails));
			}
		}

		#endregion

		#endregion

		#region Tasks Collection View Sources
		/// <summary>
		/// Initializes filters for task collection view sources.
		/// </summary>
		/// <param name="_CurrentTasksSource"></param>
		/// <param name="_HistoryTasksSource"></param>
		public void InitCollectionViewSourcesForTasks(CollectionViewSource currrentTs, CollectionViewSource historyTs)
		{
			// Makes sure that there is one and only one event handler per source.
			
			currrentTs.Filter -= new FilterEventHandler(OnCurrentTasksSourceFilter);
			currrentTs.Filter += new FilterEventHandler(OnCurrentTasksSourceFilter);
			
			historyTs.Filter -= new FilterEventHandler(OnHistoryTasksSourceFilter);
			historyTs.Filter += new FilterEventHandler(OnHistoryTasksSourceFilter);
		}

		private void OnHistoryTasksSourceFilter(object sender, FilterEventArgs e)
		{
			// Only take completed tasks.
			e.Accepted = e.Item is Task && ((Task)e.Item).Complete;
		}

		private void OnCurrentTasksSourceFilter(object sender, FilterEventArgs e)
		{
			// Only take uncompleted tasks.
			e.Accepted = e.Item is Task && !((Task)e.Item).Complete;
		} 

		#endregion
		
		protected override void InitFromNavigation(System.Windows.Navigation.NavigationContext navCtx)
		{
			base.InitFromNavigation(navCtx);

			// Tries to get a particular section to display.
			string section;
			if (navCtx.QueryString.TryGetValue(SectionKey, out section))
			{
				if (SectionValue_World.Equals(section))
				{
					PivotSelectedIndex = 1;
				}
				else if (SectionValue_Inventory.Equals(section))
				{
					PivotSelectedIndex = 2;
				}
				else if (SectionValue_Tasks.Equals(section))
				{
					PivotSelectedIndex = 3;
				}
			}

			// Nothing more to do if a cartridge exists already.
			if (Cartridge != null)
			{
				return;
			}

			// Tries to get the filename to query for.
			string filename;
			if (navCtx.QueryString.TryGetValue(CartridgeFilenameKey, out filename))
			{
				Cartridge = Model.Core.InitAndStartCartridge(filename);
			}

			// TODO: Cancel nav if no cartridge in parameter?

		}

		/// <summary>
		/// Makes the app show the details of a thing.
		/// </summary>
		/// <param name="t"></param>
		private void ShowDetails(UIObject t)
		{
			// Navigates to the appropriate view.
			App.Current.ViewModel.NavigateToView(t);
		}
	}
}
