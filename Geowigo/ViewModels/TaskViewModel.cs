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
using System.Text;

namespace Geowigo.ViewModels
{
	public class TaskViewModel : BaseViewModel
	{
		#region Dependency Properties

		#region StatusText



		public string StatusText
		{
			get { return (string)GetValue(StatusTextProperty); }
			set { SetValue(StatusTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for StatusText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StatusTextProperty =
			DependencyProperty.Register("StatusText", typeof(string), typeof(TaskViewModel), new PropertyMetadata(null));



		#endregion

		#endregion

		#region Properties

		#region WherigoObject

		public new Task WherigoObject
		{
			get
			{
				return (Task)base.WherigoObject;
			}
		}

		#endregion

		#endregion

		protected override void OnWherigoObjectChanged(Table table)
		{
			if (table != null)
			{
				RefreshStatusText();
			}
		}

		protected override void OnWherigoObjectPropertyChanged(string propName)
		{
			if (propName == "CorrectState" || propName == "Complete")
			{
				RefreshStatusText();
			}
		}

		private void RefreshStatusText()
		{
			StringBuilder sb = new StringBuilder();

			Task t = WherigoObject;

			// Completeness
			sb.Append(t.Complete ? "COMPLETED" : "TO DO");

			// Correctness
			if (t.Complete && t.CorrectState != TaskCorrectness.None)
			{
				sb.Append(" (");

				sb.Append(t.CorrectState == TaskCorrectness.Correct ? "CORRECT" : "INCORRECT");

				sb.Append(")");
			}

			// Refreshes the text.
			StatusText = sb.ToString();
		}
	}
}
