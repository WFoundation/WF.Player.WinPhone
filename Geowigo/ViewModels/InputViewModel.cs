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
using Geowigo.Controls;
using Microsoft.Phone.Controls;

namespace Geowigo.ViewModels
{
	public class InputViewModel : BaseViewModel
	{
		#region Dependency Properties

		#region Input
		public Input Input
		{
			get { return (Input)GetValue(WherigoObjectProperty); }
			set { SetValue(WherigoObjectProperty, value); }
		}
		#endregion

		#region Answer


		public string Answer
		{
			get { return (string)GetValue(AnswerProperty); }
			set { SetValue(AnswerProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Answer.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AnswerProperty =
			DependencyProperty.Register("Answer", typeof(string), typeof(InputViewModel), new PropertyMetadata(null));


		#endregion

		#region IsDiscardable


		public bool IsDiscardable
		{
			get { return (bool)GetValue(IsDiscardableProperty); }
			set { SetValue(IsDiscardableProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsDiscardable.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsDiscardableProperty =
			DependencyProperty.Register("IsDiscardable", typeof(bool), typeof(InputViewModel), new PropertyMetadata(false));


		#endregion

		#endregion

		#region Commands

		#region AcceptAnswerCommand

		private RelayCommand _AcceptAnswerCommand;

		/// <summary>
		/// Gets the command to execute when accepting an answer.
		/// </summary>
		public ICommand AcceptAnswerCommand
		{
			get
			{
				if (_AcceptAnswerCommand == null)
				{
					_AcceptAnswerCommand = new RelayCommand(AcceptAnswer);
				}

				return _AcceptAnswerCommand;
			}
		}

		#endregion

		#region DiscardInputCommand

		private RelayCommand _DiscardInputCommand;

		/// <summary>
		/// Gets the command to execute when the user wants to discard the Input.
		/// </summary>
		public ICommand DiscardInputCommand
		{
			get
			{
				if (_DiscardInputCommand == null)
				{
					_DiscardInputCommand = new RelayCommand(DiscardInput);
				}

				return _DiscardInputCommand;
			}
		}

		#endregion

		#endregion

		protected override void OnPageBackKeyPressOverride(System.ComponentModel.CancelEventArgs e)
		{
			// Dismisses the input.
			Input.GiveResult(null);
		}

		protected override void InitFromNavigation(NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			// Checks if this Input appears to be looping.
			// If so, a special panel is shown.
			IsDiscardable = App.Current.ViewModel.InputManager.IsLooping(Input);
		}

		#region Command Backends
		private void AcceptAnswer()
		{
			// Closes current page.
			App.Current.ViewModel.NavigationManager.NavigateBack();

			// Calls back on the input in the Dispatcher thread,
			// in order to make sure that any potential navigation 
			Input.GiveResult(Answer);
		}

		private void DiscardInput()
		{
			// Makes sure the user agrees.
			string caption = "A game input is looping";
			string message = "This input appears to be looping.\nThis probably happens because the cartridge expects you to answer the question correctly before the game can go on.\n\nTap on OK to stop playing and return to the main menu of the app. The game will not be saved, and your progress will be lost.\n\nTap on Cancel to keep on playing and try to answer the question correctly.";

			if (System.Windows.MessageBox.Show(message, caption, MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
			{
				App.Current.ViewModel.NavigationManager.NavigateToAppHome(true);
			}
		}
		#endregion
	}
}
