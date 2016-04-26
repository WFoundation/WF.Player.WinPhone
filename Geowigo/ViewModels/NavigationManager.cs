using System;
using System.Net;
using System.Windows;
using Microsoft.Phone.Controls;
using System.Linq;
using Geowigo.Models;
using WF.Player.Core;
using System.Windows.Navigation;
using WF.Player.Core.Threading;
using System.Collections.Generic;
using System.Threading;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for handling page navigation in the app.
	/// </summary>
	public class NavigationManager
	{
		#region Nested Classes

		private enum PageScope
		{
			/// <summary>
			/// Scope of a page that is outside the game and that
			/// stays in the navigation stack for the lifetime of the app.
			/// </summary>
			App,

			/// <summary>
			/// Scope of a page that is outside the game and is removed
			/// of the navigation stack as soon as it is not on-screen
			/// anymore.
			/// </summary>
			OneShot,

			/// <summary>
			/// Scope of a page that is in game.
			/// </summary>
			Game,

			/// <summary>
			/// Scope of a page that is out of the game but can occur
			/// during the game.
			/// </summary>
			GameExtra,

			Unknown
		}

		private class NavigationQueue
		{
			#region Nested Classes

			private class Job
			{
				/// <summary>
				/// Gets or sets if this job represents a forward navigation.
				/// </summary>
				public bool IsForwardNavigation { get; set; }

				/// <summary>
				/// Gets or sets the Uri of this job (only used for forward
				/// navigations).
				/// </summary>
				public Uri Uri { get; set; }

				/// <summary>
				/// Gets or sets if this job should be cancelled if its target
				/// Uri is the same as the Uri of the page currently on-screen.
				/// (Only used for forward navigations).
				/// </summary>
				public bool CancelIfDuplicate { get; set; }

				/// <summary>
				/// Gets or sets if this job should be executed preferredly
				/// by going back to the page if it exists in the back stack.
				/// (Only used for forward navigations).
				/// </summary>
				public bool PreferBackNavigation { get; set; }
			}

			#endregion
			
			#region Fields
			private object _syncRoot = new object();
			private bool _isNavigating = false;
			private List<Job> _queue = new List<Job>();
			private AppViewModel _appViewModel;
			private NavigationManager _parent;
			private PhoneApplicationFrame _rootFrame;
			#endregion

            #region Properties

            private bool IsNavigating
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _isNavigating;
                    }
                }

                set
                {
                    lock (_syncRoot)
                    {
                        _isNavigating = value;
                        //System.Diagnostics.Debug.WriteLine("NavigationManager: IsNavigating = " + value);
                    }
                }
            }

            #endregion

			#region Constructors
			public NavigationQueue(NavigationManager parent)
			{
				_parent = parent;
				_appViewModel = _parent._parent;
				_rootFrame = _parent._rootFrame;

				_rootFrame.Navigated += new NavigatedEventHandler(OnRootFrameNavigated);
				_rootFrame.NavigationFailed += new NavigationFailedEventHandler(OnRootFrameNavigationFailed);
                _rootFrame.NavigationStopped += new NavigationStoppedEventHandler(OnRootFrameNavigationStopped);
				_appViewModel.MessageBoxManager.HasMessageBoxChanged += new EventHandler(OnHasMessageBoxChanged);
			}

			#endregion

			#region Accept Jobs
			public void AcceptNavigate(Uri uri, bool cancelIfAlreadyActive, bool preferBackNav)
			{
				AcceptJobAndRun(new Job() 
				{ 
					Uri = uri, 
					IsForwardNavigation = true,
					PreferBackNavigation = preferBackNav,
					CancelIfDuplicate = cancelIfAlreadyActive
				});
			}

			public void AcceptNavigateBack()
			{
				AcceptJobAndRun(new Job() { IsForwardNavigation = false });
			}

			private void AcceptJobAndRun(Job job)
			{
				// Queues the job.
				lock (_syncRoot)
				{
					_queue.Add(job);
				}

				// Checks if the queue needs to be processed, and
				// if so, processes the next items.
				CheckAndRunAll();
			}
			#endregion

			#region Job Processing
			/// <summary>
			/// Informs this navigation queue that an external navigation
			/// event is expected. Subsequent navigation jobs will be delayed
			/// until this navigation has occured.
			/// </summary>
			public void ExpectNavigation()
			{
                IsNavigating = true;
			}

            /// <summary>
            /// Runs all jobs in queue as long as they are not delayed.
            /// </summary>
            private void CheckAndRunAll()
            {
                while (CheckAndRunNext())
                {
                    
                }
            }

            /// <summary>
            /// Checks that a next job is scheduled and ready to be run, and runs it if it can be.
            /// </summary>
            /// <returns>True if a job was run or cancelled, false if there is no next job, or if jobs are currently
            /// being delayed.</returns>
			private bool CheckAndRunNext()
			{
				// Do nothing if a message box is on-screen.
				if (_appViewModel.MessageBoxManager.HasMessageBox)
				{
                    return false;
				}

				lock (_syncRoot)
				{
					// Do nothing for now if a navigation is in progress.
                    if (IsNavigating)
					{
                        return false;
					}

					// Gets the next job.
					Job nextJob;
					nextJob = _queue.FirstOrDefault();
					if (nextJob == null)
					{
						// Nothing to do.
						return false;
					}

					// Is the job is possible to execute?
					// YES -> Runs it and removes it.
					// NO -> Cancels it.
					if (CanJobRun(nextJob))
					{
						RunJob(nextJob);
					}

					// Removes this job.
					RemoveJob(nextJob);

                    return true;
				}

			}

			private void RunJob(Job nextJob)
			{
				// We're going to navigate.
                IsNavigating = true;
				
				// If the job allows it, checks if the back stack 
				// already has a page with this Uri. If so, removes
				// all back entries before and navigates back to it.
				if (nextJob.PreferBackNavigation)
				{
					// Only navigates back if the back stack contains
					// an entry with the same Uri. If so, clears the back
					// stack to but not including this entry.
					if (this.ClearBackStackFor(nextJob.Uri))
					{
						// Navigates back to the right page.
						RunNavigateBack();
						return;
					}

					// If we're here, no similar entry was found in
					// the back stack. So let's fallback to a forward
					// navigation.
				}

				// Back or Forward navigation.
				if (nextJob.IsForwardNavigation || nextJob.PreferBackNavigation)
				{
					RunNavigateForward(nextJob.Uri);
				}
				else
				{
					RunNavigateBack();
				}
			}

			private void RunNavigateForward(Uri uri)
			{
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					// Tries to navigate according to the job.
					// Exceptions are caught and reported but ignored.
					try
					{
                        if (!_rootFrame.Navigate(uri))
                        {
                            // We're not navigating anymore.
                            IsNavigating = false;
                        }
					}
					catch (Exception ex)
					{
						// We're not navigating anymore.
                        IsNavigating = false;
                        
                        // Reports the exception.
						Utils.DebugUtils.DumpException(ex, "Error on Navigation request, handled.", dumpOnBugSenseToo: true);
					}
				});
			}

			private void RunNavigateBack()
			{
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					// Tries to navigate according to the job.
					// Exceptions are caught and reported but ignored.
					try
					{
						_rootFrame.GoBack();
					}
					catch (Exception ex)
					{
                        // We're not navigating anymore.
                        IsNavigating = false;
                        
                        // Reports the exception.
						Utils.DebugUtils.DumpException(ex, "Error on Navigation request, handled.", dumpOnBugSenseToo: true);
					}
				});
			}

			private void RemoveJob(Job nextJob)
			{
				lock (_syncRoot)
				{
					_queue.Remove(nextJob);
				}
			}

			private bool CanJobRun(Job nextJob)
			{
				// Checks if the navigation target is already on screen.
				if (nextJob.IsForwardNavigation
					&& nextJob.CancelIfDuplicate
					&& _rootFrame.CurrentSource.OriginalString == nextJob.Uri.OriginalString)
				{
					return false;
				}

				// Checks if the previous page is valid (Back navigation only).
				if (!nextJob.IsForwardNavigation && !IsPreviousPageValid())
				{
					return false;
				}

				// Checks if another navigation was scheduled after this one.
				// If so, this job cannot run because the last navigation
				// will take over and lead to a back stack conforming.
				// (This is because game pages should be left alone on the stack,
				// per Wherigo specifications.)
				bool isLastJobInQueue;
				lock (_syncRoot)
				{
					isLastJobInQueue = _queue.LastOrDefault() == nextJob;
				}
				if (!isLastJobInQueue)
				{
					return false;
				}

				// Conditions are good for the job to run.
				return true;
			}

			private bool IsPreviousPageValid()
			{
				// Checks if the previous view is not a game view.
				JournalEntry previousPage = _rootFrame.BackStack.FirstOrDefault();
				if (previousPage == null)
				{
					//System.Diagnostics.Debug.WriteLine("NavigationManager: WARNING: NavigateBack() cancelled because no page is in the stack.");

					return false;
				}
				if (!_parent.IsGameViewUri(previousPage.Source))
				{
					//System.Diagnostics.Debug.WriteLine("NavigationManager: WARNING: NavigateBack() cancelled because previous page is no game!");

					return false;
				}

				// Conditions are looking good for a back navigation.
				return true;
			}


			#endregion

			#region Back Stack Conforming

			private bool ClearBackStackFor(Uri source)
			{
				// Makes a list of back stack journal entries.
				List<JournalEntry> backStack = new List<JournalEntry>();
				try
				{
					backStack.AddRange(_rootFrame.BackStack);
				}
				catch (NullReferenceException)
				{
					// When the app comes back from tombstone, an NullReferenceException
					// is raised. If so, nothing more can be done.
					return false;
				}
				
				// Checks if the back stack has a similar Uri.
				bool hasSimilarUri = false;
				int entriesToRemove = 0;
				foreach (JournalEntry item in backStack)
				{
					if (item.Source.OriginalString.Replace("//", "/") == source.OriginalString)
					{
						hasSimilarUri = true;
						break;
					}

					entriesToRemove++;
				}

				// If needed, removes all back stack entries until this one.
				if (hasSimilarUri)
				{
					// Removes the entries.
					for (int i = 0; i < entriesToRemove; i++)
					{
						// Not at the right page yet, removes the entry.
						_rootFrame.RemoveBackEntry();
					}

					// The page at the top of the stack is now similar
					// to the source Uri. Do nothing more.
				}

				return hasSimilarUri;
			}

			private void ConformBackStack(Uri latestNavigatedUri)
			{
				// What is the scope of the latest navigated Uri?
				// Game -> Removes all non App entries before this one so that
				//		the page is the only Game page in the stack. 
				//		(Wherigo spec)
				// GameExtra -> Do not change anything.
				// App -> Removes all entries up to and including the previous
				//		entry for this page only if the stack contains this.
				// Others -> Do not change anything.
				PageScope latestPageScope = _parent.GetPageScope(latestNavigatedUri);

				if (latestPageScope == PageScope.App)
				{
					// If the page Uri can be found in the back stack,
					// clears entries up to and including the back stack.
					if (ClearBackStackFor(latestNavigatedUri))
					{
						_rootFrame.RemoveBackEntry();
					}
				}
				else if (latestPageScope == PageScope.Game)
				{
					foreach (JournalEntry entry in _rootFrame.BackStack.ToList())
					{
						// Removes the current entry if it is a game entry.
						if (_parent.GetPageScope(entry.Source) == PageScope.Game)
						{
							// Removes the entry.
							_rootFrame.RemoveBackEntry();
						}
						else
						{
							// Stop right here, we hit an App entry.
							break;
						}
					}
				}

				// If the top of the stack is a OneShot page, remove it
				// now, since it may be the last time it's possible to do it.
				JournalEntry topEntry = _rootFrame.BackStack.FirstOrDefault();
				if (topEntry != null && _parent.GetPageScope(topEntry.Source) == PageScope.OneShot)
				{
					_rootFrame.RemoveBackEntry();
				}
			} 

			#endregion

			#region Root Frame Event Handlers

			private void OnRootFrameNavigated(object sender, NavigationEventArgs e)
			{
				// Conforms the back stack.
				ConformBackStack(e.Uri);

				// No more navigation.
                IsNavigating = false;

				// Runs next jobs if any.
				CheckAndRunAll();
			}

			private void OnRootFrameNavigationFailed(object sender, NavigationFailedEventArgs e)
			{
                // No more navigation.
                IsNavigating = false;
                
                // Runs next jobs if any.
                CheckAndRunAll();
			}

            private void OnRootFrameNavigationStopped(object sender, NavigationEventArgs e)
            {
                // No more navigation.
                IsNavigating = false;

                // Runs next jobs if any.
                CheckAndRunAll();
            }

			private void OnHasMessageBoxChanged(object sender, EventArgs e)
			{
                // Runs next jobs if any.
                CheckAndRunAll();
			}
			#endregion
		}

		#endregion
		
		#region Fields

		private PhoneApplicationFrame _rootFrame;
		private AppViewModel _parent;
		private object _syncRoot = new object();
		private NavigationQueue _queue;

		#endregion

		#region Constructors
		public NavigationManager(AppViewModel parent)
		{
			_rootFrame = App.Current.RootFrame;
			_parent = parent;
			_queue = new NavigationQueue(this);
		}

		#endregion

		private PageScope GetPageScope(Uri pageUri)
		{
			string pageName = pageUri.ToString().Replace("//", "/");
			string prefix = "/Views/";

			PageScope scope = PageScope.Unknown;

			if (pageName.StartsWith(prefix + "InputPage.xaml") ||
				pageName.StartsWith(prefix + "TaskPage.xaml") ||
				pageName.StartsWith(prefix + "ThingPage.xaml"))
			{
				scope = PageScope.Game;
			}
			else if (pageName.StartsWith(prefix + "HomePage.xaml") ||
                pageName.StartsWith(prefix + "CartridgeInfoPage.xaml") || 
                pageName.StartsWith(prefix + "SettingsPage.xaml"))
			{
				scope = PageScope.App;
			}
			else if (pageName.StartsWith(prefix + "CompassCalibrationPage.xaml")
				|| pageName.StartsWith(prefix + "GameHomePage.xaml")
				|| pageName.StartsWith(prefix + "GameMapPage.xaml")
				|| pageName.StartsWith(prefix + "PlayerPage.xaml"))
			{
				scope = PageScope.GameExtra;
			}
			else if (pageName.StartsWith(prefix + "BetaLicensePage.xaml"))
			{
				scope = PageScope.OneShot;
			}

			return scope;
		}

		/// <summary>
		/// Determines if a page name corresponds to a view of the game.
		/// </summary>
		/// <param name="pageUri"></param>
		/// <returns></returns>
		public bool IsGameViewUri(Uri pageUri)
		{
			PageScope scope = GetPageScope(pageUri);
			return scope == PageScope.Game || scope == PageScope.GameExtra;
		}

		/// <summary>
		/// Navigates the app to the main page of the app.
		/// </summary>
		public void NavigateToAppHome(bool stopCurrentGame = false)
		{
			// Stops the current game if needed.
			if (stopCurrentGame && _parent.Model.Core.Cartridge != null)
			{
				try
				{
					// Tries to stop the engine the nice way.
					_parent.Model.Core.StopAndResetAsync().ContinueWith(
						t => 
							{
								if (t.IsFaulted)
								{
									// The nice way didn't work.
									// So hard reset the engine.
									_parent.HardResetCore();
								}
								
								NavigateToAppHomeCore();
							},
						System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
				}
				catch (Exception)
				{
					// The nice way didn't work.
					// So hard reset the engine.
					_parent.HardResetCore();

					// Navigates.
					NavigateToAppHomeCore();
				}
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

			// Dismisses all message boxes.
			_parent.MessageBoxManager.DismissAllMessageBoxes();

			// Navigates now.
			NavigateCore(new Uri("/Views/HomePage.xaml", UriKind.Relative), preferBackNav: true);
		}

		/// <summary>
		/// Navigates the app to the page of compass calibration.
		/// </summary>
		public void NavigateToCompassCalibration()
		{
			string pageUrl = "/Views/CompassCalibrationPage.xaml";

			// Navigates
			NavigateCore(new Uri(pageUrl, UriKind.Relative), cancelIfAlreadyActive: true);
		}

		/// <summary>
		/// Navigates the app to the page about player and device info.
		/// </summary>
		public void NavigateToPlayerInfo()
		{
			string pageUrl = "/Views/PlayerPage.xaml";

			// Navigates
			NavigateCore(new Uri(pageUrl, UriKind.Relative), cancelIfAlreadyActive: true);
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
		/// Navigates the app to the map of the game zone.
		/// </summary>
		public void NavigateToGameMap()
		{
			NavigateCore(new Uri("/Views/GameMapPage.xaml", UriKind.Relative), true);
		}

		/// <summary>
		/// Navigates the app to the info page of a cartridge.
		/// </summary>
		public void NavigateToCartridgeInfo(CartridgeTag tag)
		{
			NavigateCore(new Uri(String.Format("/Views/CartridgeInfoPage.xaml?{0}={1}&{2}={3}", CartridgeInfoViewModel.CartridgeFilenameKey, tag.Cartridge.Filename, CartridgeInfoViewModel.CartridgeIdKey, tag.Guid), UriKind.Relative));
		}

        /// <summary>
        /// Navigates the app to the settings page.
        /// </summary>
        public void NavigateToSettings()
        {
            NavigateCore(new Uri("/Views/SettingsPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Navigates the app to the wizard of linking a provider.
        /// </summary>
        public void NavigateToProviderLinkWizard(Models.Providers.ICartridgeProvider provider)
        {
            if (provider == null || provider.GetType() != typeof(Models.Providers.OneDriveCartridgeProvider))
            {
                throw new NotSupportedException();
            }
            
            NavigateCore(new Uri(String.Format("/Views/SettingsPage.xaml?{0}={1}&{2}={3}", SettingsViewModel.ProviderServiceNameKey, provider.ServiceName, SettingsViewModel.ProviderWizardKey, Boolean.TrueString), UriKind.Relative));
        }


		/// <summary>
		/// Navigates the app to the view that best fits an Input object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Input wherigoObj)
		{
			NavigateCore(new Uri("/Views/InputPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative), cancelIfAlreadyActive: true);
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Thing object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Thing wherigoObj)
		{
			// If the thing is the player, navigates to device and player info view.
			if (WherigoObject.AreSameEntities(wherigoObj, _parent.Model.Core.Player))
			{
				NavigateToPlayerInfo();
			}
			else
			{
                NavigateCore(new Uri("/Views/ThingPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative), cancelIfAlreadyActive: true);
			}
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Task object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Task wherigoObj)
		{
			NavigateCore(new Uri("/Views/TaskPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative), cancelIfAlreadyActive: true);
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
		public void NavigateBack()
		{
			// Goes back.
			_queue.AcceptNavigateBack();
		}

		/// <summary>
		/// Delays all navigation jobs until an external navigation
		/// occurs.
		/// </summary>
		public void PauseUntilNextNavigation()
		{
			_queue.ExpectNavigation();
		}

		private void NavigateCore(Uri source, bool cancelIfAlreadyActive = true, bool preferBackNav = false)
		{
			_queue.AcceptNavigate(source, cancelIfAlreadyActive, preferBackNav);
		}

	}
}
