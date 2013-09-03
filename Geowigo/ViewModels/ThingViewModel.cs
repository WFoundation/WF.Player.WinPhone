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

		#region Properties

		#region WherigoObject

		public new UIObject WherigoObject
		{
			get
			{
				return (UIObject)base.WherigoObject;
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
			return true;
		}

	}
}
