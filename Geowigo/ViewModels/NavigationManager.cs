using System;
using System.Net;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Linq;
using Geowigo.Models;
using WF.Player.Core;
using System.Windows.Navigation;
using WF.Player.Core.Threading;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for handling page navigation in the app.
	/// </summary>
	public class NavigationManager
	{
		#region Fields

		private PhoneApplicationFrame _rootFrame;
		private AppViewModel _parent;
		private ActionPump _navigationPump;

		#endregion

		#region Constructors
		public NavigationManager(AppViewModel parent)
		{
			_rootFrame = App.Current.RootFrame;
			_parent = parent;
			_navigationPump = new ActionPump();
		} 
		#endregion

		/// <summary>
		/// Navigates the app to the main page of the app.
		/// </summary>
		public void NavigateToAppHome(bool stopCurrentGame = false)
		{
			// Stops the current game if needed.
			if (stopCurrentGame && _parent.Model.Core.Cartridge != null)
			{
				_parent.Model.Core.StopAndResetAsync().ContinueWith(
					t => NavigateToAppHomeCore(),
					System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
			}
			else
			{
				NavigateToAppHomeCore();
			}
		}

		private void NavigateToAppHomeCore()
		{
			// Resets the custom system tray status.
			_parent.SystemTrayManager.StatusText = null;

			// Resets the input tracking.
			_parent.InputManager.Reset();

			// Removes all back entries until the app home is found.
			string prefix = "/Views/";
			foreach (JournalEntry entry in _rootFrame.BackStack.ToList())
			{
				if (entry.Source.ToString().StartsWith(prefix + "HomePage.xaml"))
				{
					break;
				}

				// Removes the current entry.
				_rootFrame.RemoveBackEntry();
			}

			// If there is a back entry, goes back: it is the game home.
			// Otherwise, navigates to the game home.
			if (_rootFrame.BackStack.Count() > 0)
			{
				//_rootFrame.GoBack();
				GoBackCore();
			}
			else
			{
				try
				{
					NavigateCore(new Uri(prefix + "HomePage.xaml", UriKind.Relative));
				}
				catch (InvalidOperationException ex)
				{
					// This is probably due to a race condition between two navigation
					// requests, one being this one, and the other likely to be an external
					// task navigation (map task, etc.)
					// There is nothing to do: the user will surely be luckier next time.
					Utils.DebugUtils.DumpException(ex, dumpOnBugSenseToo: true);
				}
			}
		}

		/// <summary>
		/// Navigates the app to the page of compass calibration.
		/// </summary>
		public void NavigateToCompassCalibration()
		{
			string pageUrl = "/Views/CompassCalibrationPage.xaml";

			// Discards the request if the current page is already the compass calibration.
			if (_rootFrame.CurrentSource.OriginalString.StartsWith(pageUrl))
			{
				return;
			}

			// Navigates
			NavigateCore(new Uri(pageUrl, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge.
		/// </summary>
		public void NavigateToGameHome(string filename)
		{
			NavigateCore(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}", GameHomeViewModel.CartridgeFilenameKey, filename), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge at a specific section.
		/// </summary>
		public void NavigateToGameHome(string filename, string section)
		{
			NavigateCore(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}&{2}={3}", GameHomeViewModel.CartridgeFilenameKey, filename, GameHomeViewModel.SectionKey, section), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge and restores a
		/// savegame.
		/// </summary>
		public void NavigateToGameHome(string filename, CartridgeSavegame savegame)
		{
			NavigateCore(new Uri(String.Format(
				"/Views/GameHomePage.xaml?{0}={1}&{2}={3}",
				GameHomeViewModel.CartridgeFilenameKey,
				filename,
				GameHomeViewModel.SavegameFilenameKey,
				savegame.SavegameFile), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the info page of a cartridge.
		/// </summary>
		public void NavigateToCartridgeInfo(CartridgeTag tag)
		{
			NavigateCore(new Uri(String.Format("/Views/CartridgeInfoPage.xaml?{0}={1}&{2}={3}", CartridgeInfoViewModel.CartridgeFilenameKey, tag.Cartridge.Filename, CartridgeInfoViewModel.CartridgeIdKey, tag.Guid), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits an Input object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Input wherigoObj)
		{
			NavigateCore(new Uri("/Views/InputPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Thing object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Thing wherigoObj)
		{
			NavigateCore(new Uri("/Views/ThingPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Task object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Task wherigoObj)
		{
			NavigateCore(new Uri("/Views/TaskPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a UIObject object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(UIObject wherigoObj)
		{
			if (wherigoObj is Thing)
			{
				NavigateToView((Thing)wherigoObj);
			}
			else if (wherigoObj is Task)
			{
				NavigateToView((Task)wherigoObj);
			}
		}

		/// <summary>
		/// Navigates one step back in the game activity.
		/// </summary>
		/// <remarks>
		/// This method has no effect if the previous view in the stack is not a game view.
		/// </remarks>
		/// <param name="currentExpectedView">The name of the view source is expected
		/// to be displayed. If non-null, and the current view is not the same as
		/// the <paramref name="currentExpectedView"/>, the current expected view
		/// is removed from the back stack, and the navigation is cancelled.</param>
		public void NavigateBack(Uri currentExpectedView = null)
		{
			// Returns if the previous view is not a game view.
			JournalEntry previousPage = _rootFrame.BackStack.FirstOrDefault();
			if (previousPage == null)
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: WARNING: NavigateBack() cancelled because no page is in the stack.");

				return;
			}
			if (!IsGameViewUri(previousPage.Source))
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: WARNING: NavigateBack() cancelled because previous page is no game!");

				return;
			}

			// Returns if the current view is not expected.
			// This means that a navigation happened between this navigate back
			// was originally triggered and now.
			// When this happens, the back stack needs to be cleared of this expected
			// view if possible, and then the method should return.
			if (currentExpectedView != null && currentExpectedView != _rootFrame.CurrentSource)
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: WARNING: NavigateBack() cancelled because a navigation occured after the call.");

				return;
			}

			// Do not navigate back if a message box is on-screen.
			// Instead, delay the navigation.
			if (_parent.MessageBoxManager.HasMessageBox)
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: WARNING: NavigateBack() delayed because a message box is on-screen!");

				_parent.BeginRunOnIdle(new Action(() => NavigateBack(currentExpectedView ?? _rootFrame.CurrentSource)));

				return;
			}

			// Goes back.
			GoBackCore();
		}

		/// <summary>
		/// Determines if a page name corresponds to a view of the game.
		/// </summary>
		/// <param name="pageUri"></param>
		/// <returns></returns>
		public bool IsGameViewUri(Uri pageUri)
		{
			string pageName = pageUri.ToString();
			string prefix = "/Views/";
			return pageName.StartsWith(prefix + "GameHomePage.xaml") ||
				pageName.StartsWith(prefix + "InputPage.xaml") ||
				pageName.StartsWith(prefix + "TaskPage.xaml") ||
				pageName.StartsWith(prefix + "ThingPage.xaml");
		}

		/// <summary>
		/// Clears the navigation back stack, making the current view the first one.
		/// </summary>
		public void ClearBackStack()
		{
			int entriesToRemove = _rootFrame.BackStack.Count();
			for (int i = 0; i < entriesToRemove; i++)
			{
				_rootFrame.RemoveBackEntry();
			}
		}

		#region Core Navigation Wrapper
		private void NavigateCore(Uri source)
		{
			RunInUIDispatcherFromPump(() => _rootFrame.Navigate(source));
		}

		private void GoBackCore()
		{
			RunInUIDispatcherFromPump(() => _rootFrame.GoBack());
		}

		private void RunInUIDispatcherFromPump(Action action)
		{
			// Adds a job to the action pump.
			_navigationPump.AcceptAction(() =>
			{
				// This runs in the action pump thread.
				// Triggers the navigation in the UI dispatcher.
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					action();
				});
			});

			// Makes sure the pump is running.
			_navigationPump.IsPumping = true;
		}
		#endregion

	}
}
