﻿using System;
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

		#region IsCompassVisible


		public bool IsCompassVisible
		{
			get { return (bool)GetValue(IsCompassVisibleProperty); }
			set { SetValue(IsCompassVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsCompassVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsCompassVisibleProperty =
			DependencyProperty.Register("IsCompassVisible", typeof(bool), typeof(ThingViewModel), new PropertyMetadata(false));

		
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

		#region Fields

		private Uri _startUri;

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

		protected override void InitFromNavigation(NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			_startUri = nav.Uri;
		}

		protected override void OnPageNavigatedBackToOverride()
		{
			// Makes sure the page still should be shown.
			CheckShouldNavigateBack();
		}

		private void CheckShouldNavigateBack(bool containerChanged = false)
		{
			bool shouldGoBack = false;

			if (WherigoObject == null)
			{
				// Go back if this view has no associated wherigo object
				// because this is an unexpected case.
				shouldGoBack = true;
			}
			else
			{
				Thing cont = WherigoObject.Container;
				shouldGoBack = _startUri != null && IsPageVisible && // The page must be visible to be able to trigger a navigation.
					(!WherigoObject.Visible // The object is not supposed to be visible.
					|| (WherigoObject is Zone && !((Zone)WherigoObject).Active) // The zone is not active.
					|| (cont == null && containerChanged) // The item or character has no container.
					|| (cont != null && cont != Model.Core.Player && !cont.Visible) // The container is invisible.
					);
			}

			if (shouldGoBack)
			{
				App.Current.ViewModel.NavigationManager.NavigateBackOrForget(_startUri);
			}
		}

		#region Handling of ZThing properties change

		protected override void OnWherigoObjectPropertyChanged(string propName)
		{
			if ("ActiveCommands".Equals(propName))
			{
				// Refreshes the visibilities.
				RefreshActionVisibilities();
			}
			else if ("Visible".Equals(propName) || "Active".Equals(propName))
			{
				// If this Thing is not active-visible anymore, get back to previous screen.
				CheckShouldNavigateBack();
			}
			else if ("Container".Equals(propName))
			{
				// Refreshes the visiblities.
				RefreshContainerVisibilities();
				
				// If this thing is not in the Player or a visible Thing's inventory 
				// anymore, return to previous page.
				CheckShouldNavigateBack(containerChanged: true);

				// Refreshes the status text.
				RefreshStatusText();
			}
			else if ("VectorFromPlayer".Equals(propName))
			{
				RefreshStatusText();
			}
		}

        protected override void OnWherigoObjectChanged(WherigoObject obj)
		{
			RefreshActionVisibilities();
			RefreshContainerVisibilities();
			RefreshStatusText();
		}

		private void RefreshStatusText()
		{
			// Updates the status text depending on the type of the object.
			// Zone -> displays state of the zone regarding the player.
			// Character -> displays if by a zone or with thing.
			// Thing -> displays if in a zone or with thing.

			if (WherigoObject is Zone)
			{
				// Updates the status text depending on the zone state.
				switch (((Zone)WherigoObject).State)
				{
					case PlayerZoneState.Inside:
						StatusText = "Player is inside";
						break;

					case PlayerZoneState.Proximity:
						StatusText = "In proximity";
						break;

					case PlayerZoneState.Distant:
						StatusText = "Distant";
						break;

					case PlayerZoneState.NotInRange:
						StatusText = "Too far away";
						break;

					default:
						StatusText = "Unknown Location";
						break;
				}
			}
			else
			{
				// Updates the status text depending on the container.
				Thing container = WherigoObject.Container;
				if (container == null)
				{
					StatusText = "Unknown Location";
				}
				else if (container == Model.Core.Player)
				{
					if (WherigoObject is Character)
					{
						StatusText = "With Player";
					}
					else
					{
						StatusText = "In Player Inventory";
					}
				}
				else
				{
					string adverb;
					string typeName = container.GetType().Name;
					
					if (container is Zone)
					{
						adverb = "By";
					}
					else if (container is Item)
					{
						adverb = "In";
					}
					else
					{
						adverb = "With";
					}

					StatusText = String.Format("{0} {1}", adverb, container.Name ?? String.Format("Unnamed {0}", typeName));
				}
			}
		}

		private void RefreshActionVisibilities()
		{
			AreActionsVisible = WherigoObject.ActiveCommands.Count > 0;
		}

		private void RefreshContainerVisibilities()
		{
			IsCompassVisible = WherigoObject.Container != Model.Core.Player && WherigoObject.VectorFromPlayer != null;
		}

		#endregion
	}
}
