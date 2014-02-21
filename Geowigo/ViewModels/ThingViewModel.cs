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
using Geowigo.Controls;
using WF.Player.Core;
using Microsoft.Phone.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Geowigo.ViewModels
{	
	public class ThingViewModel : BaseViewModel
	{
		#region Classes

		/// <summary>
		/// Event args for a request to prompt the user for a command target.
		/// </summary>
		public class CommandTargetRequestedEventArgs : EventArgs
		{
			internal CommandTargetRequestedEventArgs(Command command, IEnumerable<Thing> allTargets, Action<Command, Thing> callback)
			{
				AllCommandTargets = allTargets;
				_GotTargetCallback = callback;
				Command = command;
			}

			private Action<Command, Thing> _GotTargetCallback;

			/// <summary>
			/// Gets the command.
			/// </summary>
			public Command Command { get; private set; }

			/// <summary>
			/// Gets the possible command targets.
			/// </summary>
			public IEnumerable<Thing> AllCommandTargets { get; private set; }

			/// <summary>
			/// Sets the result of this request event.
			/// </summary>
			/// <param name="commandTarget"></param>
			public void GiveResult(Thing commandTarget)
			{
				_GotTargetCallback(this.Command, commandTarget);
			}
		}

		#endregion

		#region Commands

		#region ExecuteCommand

		private ICommand _ExecuteCommand;

		/// <summary>
		/// Gets a command to run a Wherigo command.
		/// </summary>
		public ICommand ExecuteCommand
		{
			get
			{
				return _ExecuteCommand ?? (_ExecuteCommand = new RelayCommand<Command>(ExecuteWherigoCommand, CanWherigoCommandExecute));
			}
		}

		#endregion

		#endregion

		#region Dependency Properties

		#region AreActionsVisible


		public bool AreActionsVisible
		{
			get { return (bool)GetValue(AreActionsVisibleProperty); }
			set { SetValue(AreActionsVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreActionsVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreActionsVisibleProperty =
			DependencyProperty.Register("AreActionsVisible", typeof(bool), typeof(ThingViewModel), new PropertyMetadata(false));

		
		#endregion

		#region StatusText


		public string StatusText
		{
			get { return (string)GetValue(StatusTextProperty); }
			set { SetValue(StatusTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for StatusText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StatusTextProperty =
			DependencyProperty.Register("StatusText", typeof(string), typeof(ThingViewModel), new PropertyMetadata(null));


		#endregion

		#endregion

		#region Properties

		#region WherigoObject

		public new Thing WherigoObject
		{
			get
			{
				return (Thing)base.WherigoObject;
			}
		}

		#endregion

		#endregion

		#region Events
		
		/// <summary>
		/// Raised when a target for a command needs to be prompted to the user.
		/// </summary>
		public event EventHandler<CommandTargetRequestedEventArgs> CommandTargetRequested;

		#endregion

		#region ZCommand execution
		private void RaiseCommandTargetRequested(Command command)
		{
			// Creates the event args and raises the event.
			CommandTargetRequestedEventArgs e = new CommandTargetRequestedEventArgs(command, new List<Thing>(command.TargetObjects), ExecuteWherigoCommandOnTarget);
			if (CommandTargetRequested == null)
			{
				throw new InvalidOperationException("No event handler for CommandTargetRequested is registered!");
			}
			else
			{
				CommandTargetRequested(this, e);
			}
		}

		private void ExecuteWherigoCommandOnTarget(Command command, Thing target)
		{
			command.Execute(target);
		}

		private void ExecuteWherigoCommand(Command command)
		{
			if (command.CmdWith)
			{
				RaiseCommandTargetRequested(command);
			}
			else
			{
				command.Execute();
			}
		}

		private bool CanWherigoCommandExecute(Command command)
		{
			return command.Enabled;
		} 
		#endregion

		#region Handling of ZThing properties change

		protected override void OnWherigoObjectPropertyChanged(string propName)
		{
			if ("ActiveCommands".Equals(propName))
			{
				// Refreshes the visibilities.
				RefreshVisibilities();
			}
			else if ("Visible".Equals(propName) || "Active".Equals(propName))
			{
				// If this Thing is not active-visible anymore, get back to previous screen.
				if (!WherigoObject.Visible || (WherigoObject is Zone && !((Zone)WherigoObject).Active))
				{
					App.Current.ViewModel.NavigateBack();
				}
			}
			else if ("Container".Equals(propName))
			{
				// If this thing is not in the Player or a visible Thing's inventory 
				// anymore, return to previous page.
				Thing cont = WherigoObject.Container;
				if (cont == null || (cont != Model.Core.Player && !cont.Visible))
				{
					App.Current.ViewModel.NavigateBack();
				}

				// Refreshes the status text.
				RefreshStatusText();
			}
		}

        protected override void OnWherigoObjectChanged(WherigoObject obj)
		{
			RefreshVisibilities();
			RefreshStatusText();
		}

		private void RefreshStatusText()
		{
			Thing container = WherigoObject.Container;

			// Updates the status text depending on the container.
			if (container == null)
			{
				StatusText = "Unknown Location";
			}
			else if (container == Model.Core.Player)
			{
				StatusText = "In Player Inventory";
			}
			else
			{
				StatusText = "With " + container.Name ?? "Unnamed Thing";
			}
		}

		private void RefreshVisibilities()
		{
			AreActionsVisible = WherigoObject.ActiveCommands.Count > 0;
		}

		#endregion

	}
}
