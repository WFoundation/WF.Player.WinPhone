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
using System.IO.IsolatedStorage;
using Geowigo.Models;
using System.Collections.Generic;
using Microsoft.Phone.Controls;
using System.IO;

namespace Geowigo.ViewModels
{	
	/// <summary>
	/// The application view model, which is responsible for application-wide flow and control of the app and game UI.
	/// </summary>
	public class AppViewModel
	{

		#region Fields

		private MessageBoxManager _MBManagerInstance;

		#endregion

		#region Properties

		#region Model
		/// <summary>
		/// Gets or sets the wherigo model used by this ViewModel.
		/// </summary>
		public WherigoModel Model 
		{
			get
			{
				return _Model;
			}
			set
			{
				if (_Model != value)
				{
					// Unregisters the current model.
					if (_Model != null)
					{
						UnregisterModel(_Model);
					}

					// Changes the model.
					_Model = value;

					// Registers the new model.
					if (_Model != null)
					{
						RegisterModel(_Model);
					}
				}
			}
		}

		private WherigoModel _Model;
		#endregion

		#region AppTitle

		/// <summary>
		/// Gets the title of the application.
		/// </summary>
		public string AppTitle
		{
			get
			{
				return "Geowigo Wherigo Player";
			}
		}

		#endregion

		#region MessageBoxManager

		/// <summary>
		/// Gets the message box manager for this view model.
		/// </summary>
		public MessageBoxManager MessageBoxManagerInstance
		{
			get
			{
				return _MBManagerInstance ?? (_MBManagerInstance = new MessageBoxManager());
			}
		}

		#endregion

		#endregion

		#region Constructors

		
		#endregion

		#region Public Methods

		/// <summary>
		/// Navigates the app to the main page of the app.
		/// </summary>
		public void NavigateToAppHome()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge.
		/// </summary>
		public void NavigateToGameHome(string filename)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}", GameHomeViewModel.CartridgeFilenameKey, filename), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge at a specific section.
		/// </summary>
		public void NavigateToGameHome(string filename, string section)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}&{2}={3}", GameHomeViewModel.CartridgeFilenameKey, filename, GameHomeViewModel.SectionKey, section), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits an Input object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Input wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/InputPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Thing object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Thing wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/ThingPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Task object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Task wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/TaskPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
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
		/// Displays a message box. If a message box is currently on-screen, it will be cancelled.
		/// </summary>
		/// <param name="mbox"></param>
		public void ShowMessageBox(WF.Player.Core.MessageBox mbox)
		{
			// Delegates this to the message box manager.
			MessageBoxManagerInstance.Show(mbox);
		}

		/// <summary>
		/// Navigates one step back.
		/// </summary>
		public void NavigateBack()
		{
			App.Current.RootFrame.GoBack();
		}

		#endregion

		#region Private Methods

		private void RegisterModel(WherigoModel model)
		{
			model.Core.InputRequested += new EventHandler<ObjectEventArgs<Input>>(Core_InputRequested);
			model.Core.ShowMessageBoxRequested += new EventHandler<MessageBoxEventArgs>(Core_MessageBoxRequested);
			model.Core.ShowScreenRequested += new EventHandler<ScreenEventArgs>(Core_ScreenRequested);
			model.Core.PlayMediaRequested += new EventHandler<ObjectEventArgs<Media>>(Core_PlaySoundRequested);
		}

		private void UnregisterModel(WherigoModel model)
		{
			model.Core.InputRequested -= new EventHandler<ObjectEventArgs<Input>>(Core_InputRequested);
			model.Core.ShowMessageBoxRequested -= new EventHandler<MessageBoxEventArgs>(Core_MessageBoxRequested);
			model.Core.ShowScreenRequested -= new EventHandler<ScreenEventArgs>(Core_ScreenRequested);
			model.Core.PlayMediaRequested -= new EventHandler<ObjectEventArgs<Media>>(Core_PlaySoundRequested);
		}

		private void PlayMediaSound(Media media)
		{
			//using (var stream = new MemoryStream(media.Data))
			//{
			//    var effect = SoundEffect.FromStream(stream);
			//    FrameworkDispatcher.Update();
			//    effect.Play();
			//}
		}

		#region Core Event Handlers

		private void Core_InputRequested(object sender, ObjectEventArgs<Input> e)
		{
			// Navigates to the input view.
			NavigateToView(e.Object);
		}

		private void Core_MessageBoxRequested(object sender, MessageBoxEventArgs e)
		{
			// Displays the message box.
			ShowMessageBox(e.Descriptor);
		}

		private void Core_ScreenRequested(object sender, ScreenEventArgs e)
		{
			// Shows the right screen depending on the event.
			switch (e.Screen)
			{
				case ScreenType.Main:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Overview);
					break;

				case ScreenType.Locations:
				case ScreenType.Items:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_World);
					break;

				case ScreenType.Inventory:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Inventory);
					break;

				case ScreenType.Tasks:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Tasks);
					break;

				case ScreenType.Details:
					NavigateToView(e.Object);
					break;

				default:
					throw new InvalidOperationException(String.Format("Unknown WherigoScreenKind cannot be processed: {0}", e.Screen.ToString()));
			}
		}

		private void Core_PlaySoundRequested(object sender, ObjectEventArgs<Media> e)
		{
			// TODO: Pass to SoundManager for uncompressing to isolated storage and playing using a MediaElement?
			//PlayMediaSound(e.Object);
		}


		#endregion

		#endregion
	}
}
