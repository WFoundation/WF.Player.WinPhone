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

namespace Geowigo.Views
{
	public partial class InputPage : BasePage
	{
		#region Properties

		public new InputViewModel ViewModel
		{
			get
			{
				return (InputViewModel) base.ViewModel;
			}
			set
			{
				base.ViewModel = value;
			}
		}

		#endregion
		
		public InputPage()
		{
			InitializeComponent();
		}

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			ViewModel.OnPageBackKeyPress(e);

			base.OnBackKeyPress(e);
		}
	}
}