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

		#endregion

		private void AcceptAnswer()
		{			
			// Closes current page.
			App.Current.ViewModel.NavigateBack();

			// Calls back on the input.
			Input.GiveResult(Answer);
		}
	}
}
