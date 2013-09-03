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
	public partial class ThingPage : BasePage
	{

		#region Properties

		#region ViewModel

		public new ThingViewModel ViewModel
		{
			get
			{
				return (ThingViewModel)base.ViewModel;
			}
			set
			{
				base.ViewModel = value;
			}
		}

		#endregion

		#endregion

		#region Fields

		private ListPicker _CommandTargetListPicker;

		#endregion

		public ThingPage()
		{
			InitializeComponent();

			// Fields.
			_CommandTargetListPicker = (ListPicker)this.FindName("CommandTargetListPicker");

			// Event handlers.
			RegisterListBoxHelpers(this.CommandList);
			ViewModel.CommandTargetRequested += new EventHandler<ThingViewModel.CommandTargetRequestedEventArgs>(ViewModel_CommandTargetRequested);
		}

		protected override void OnListBoxSelectionChangedOverride(ListBox lb, SelectionChangedEventArgs e)
		{
			if (lb == CommandList)
			{
				// Gets the first selected item to navigate to.
				Command command = e.AddedItems.OfType<Command>().FirstOrDefault();

				// Runs the command.
				ViewModel.ExecuteCommand.Execute(command);
			}
		}

		#region Command Target Selection
		private void ViewModel_CommandTargetRequested(object sender, ThingViewModel.CommandTargetRequestedEventArgs e)
		{
			// Tags the list picker with the current command for latter use.
			_CommandTargetListPicker.Tag = e;

			// Creates the list picker collection with a first collapsed null item,
			// because list pickers always have a selected value.
			_CommandTargetListPicker.Items.Clear();
			_CommandTargetListPicker.Items.Add("");
			foreach (var item in e.AllCommandTargets)
			{
				_CommandTargetListPicker.Items.Add(item);
			}
			_CommandTargetListPicker.SelectedIndex = 0;

			// Starts listening to the list picker selection changed event.
			_CommandTargetListPicker.SelectionChanged -= new SelectionChangedEventHandler(CommandTargetListPicker_SelectionChanged);
			_CommandTargetListPicker.SelectionChanged += new SelectionChangedEventHandler(CommandTargetListPicker_SelectionChanged);

			// Sets event handlers and opens the picker. Cancel event handlers if opening failed.
			// Make sure that only one event handler is registered.
			_CommandTargetListPicker.SelectionChanged -= new SelectionChangedEventHandler(CommandTargetListPicker_SelectionChanged);
			_CommandTargetListPicker.SelectionChanged += new SelectionChangedEventHandler(CommandTargetListPicker_SelectionChanged);
			if (!_CommandTargetListPicker.Open())
			{
				_CommandTargetListPicker.SelectionChanged -= new SelectionChangedEventHandler(CommandTargetListPicker_SelectionChanged);
			}

		}

		private void CommandTargetListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Gets the target of the command.
			Thing target = e.AddedItems.OfType<Thing>().FirstOrDefault();

			// Stops listening to the picker selection changed event.
			_CommandTargetListPicker.SelectionChanged -= new SelectionChangedEventHandler(CommandTargetListPicker_SelectionChanged);

			// Gives result.
			if (target == null)
			{
				throw new InvalidOperationException("ListPicker selection changed but no Thing was selected.");
			}
			((ThingViewModel.CommandTargetRequestedEventArgs)_CommandTargetListPicker.Tag).GiveResult(target);
		} 
		#endregion
	}
}