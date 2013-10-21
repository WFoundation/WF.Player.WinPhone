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

		#region AreZonesVisible


		public bool AreZonesVisible
		{
			get { return (bool)GetValue(AreZonesVisibleProperty); }
			set { SetValue(AreZonesVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreZonesVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreZonesVisibleProperty =
			DependencyProperty.Register("AreZonesVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));

		
		#endregion

		#region AreObjectsVisible


		public bool AreObjectsVisible
		{
			get { return (bool)GetValue(AreObjectsVisibleProperty); }
			set { SetValue(AreObjectsVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreObjectsVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreObjectsVisibleProperty =
			DependencyProperty.Register("AreObjectsVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region AreCurrentTasksVisible


		public bool AreCurrentTasksVisible
		{
			get { return (bool)GetValue(AreCurrentTasksVisibleProperty); }
			set { SetValue(AreCurrentTasksVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreCurrentTasksVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreCurrentTasksVisibleProperty =
			DependencyProperty.Register("AreCurrentTasksVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region AreHistoryTasksVisible


		public bool AreHistoryTasksVisible
		{
			get { return (bool)GetValue(AreHistoryTasksVisibleProperty); }
			set { SetValue(AreHistoryTasksVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreHistoryTasksVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreHistoryTasksVisibleProperty =
			DependencyProperty.Register("AreHistoryTasksVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region IsWorldEmpty


		public bool IsWorldEmpty
		{
			get { return (bool)GetValue(IsWorldEmptyProperty); }
			set { SetValue(IsWorldEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsWorldEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsWorldEmptyProperty =
			DependencyProperty.Register("IsWorldEmpty", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(true));


		#endregion

		#region IsInventoryEmpty


		public bool IsInventoryEmpty
		{
			get { return (bool)GetValue(IsInventoryEmptyProperty); }
			set { SetValue(IsInventoryEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsInventoryEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsInventoryEmptyProperty =
			DependencyProperty.Register("IsInventoryEmpty", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(true));


		#endregion

		#region IsTasksEmpty

		public bool IsTasksEmpty
		{
			get { return (bool)GetValue(IsTasksEmptyProperty); }
			set { SetValue(IsTasksEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsTasksEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsTasksEmptyProperty =
			DependencyProperty.Register("IsTasksEmpty", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(true));

		
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

		#region ShowSectionCommand

		private ICommand _ShowSectionCommand;

		/// <summary>
		/// Gets a command to show the details of a thing.
		/// </summary>
		public ICommand ShowSectionCommand
		{
			get
			{
				return _ShowSectionCommand ?? (_ShowSectionCommand = new RelayCommand<FrameworkElement>(ShowSectionFromTag));
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

		public void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			// Cancels the back key event.
			e.Cancel = true;
			
			// Ask if we really want to leave the game.
			if (System.Windows.MessageBox.Show("Do you want to quit the game? (Your unsaved progress will be lost.)", "Exit to main menu?", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
			{
				// Leaves to main menu and stops the game.
				App.Current.ViewModel.NavigateToAppHome(true);
			}
		}

		protected override void OnCorePropertyChanged(string propName)
		{
			// Refreshes all visibilities if the model is now ready. Otherwise tries our best
			// with the prop name we get.
			RefreshVisibilities("IsReady".Equals(propName) && Model.Core.IsReady ? null : propName);
		}

		protected override void OnModelChanged(Models.WherigoModel newModel)
		{
			if (newModel.Core.IsReady)
			{
				// Refreshes all visibilities.
				RefreshVisibilities();
			}
		}
		
		protected override void InitFromNavigation(System.Windows.Navigation.NavigationContext navCtx)
		{
			base.InitFromNavigation(navCtx);

			// Tries to get a particular section to display.
			string section;
			if (navCtx.QueryString.TryGetValue(SectionKey, out section))
			{
				ShowSection(section);
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

		/// <summary>
		/// Shows a particular section of the view.
		/// </summary>
		/// <param name="section"></param>
		private void ShowSection(string section)
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

		/// <summary>
		/// Shows a particular section of the view from the tag of a FrameworkElement.
		/// </summary>
		/// <param name="element"></param>
		private void ShowSectionFromTag(FrameworkElement element)
		{
			if (element == null)
			{
				return;
			}

			ShowSection(element.Tag as string);
		}

		/// <summary>
		/// Refreshes the visibility of one or all wherigo object panels.
		/// </summary>
		/// <param name="propName"></param>
		private void RefreshVisibilities(string propName = null)
		{
			bool refreshAll = propName == null;
			bool refreshWorldEmpty = false;

			if (refreshAll || "ActiveVisibleZones".Equals(propName))
			{
				AreZonesVisible = Model.Core.ActiveVisibleZones.Count > 0;
				refreshWorldEmpty = true;
			}

			if (refreshAll || "VisibleObjects".Equals(propName))
			{
				AreObjectsVisible = Model.Core.VisibleObjects.Count > 0;
				refreshWorldEmpty = true;
			}

			if (refreshAll || "ActiveVisibleTasks".Equals(propName))
			{
				int incompleteTasksCount = Model.Core.ActiveVisibleTasks.Select(t => !t.Complete).Count();

				AreCurrentTasksVisible = incompleteTasksCount > 0;
				AreHistoryTasksVisible = Model.Core.ActiveVisibleTasks.Count - incompleteTasksCount > 0;

				IsTasksEmpty = !AreCurrentTasksVisible && !AreHistoryTasksVisible;
			}

			if (refreshAll || "VisibleInventory".Equals(propName))
			{
				IsInventoryEmpty = Model.Core.VisibleInventory.Count <= 0;
			}

			if (refreshAll || refreshWorldEmpty)
			{
				IsWorldEmpty = !AreZonesVisible && !AreObjectsVisible;
			}
		}
	}
}
