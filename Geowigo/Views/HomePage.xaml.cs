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
using System.Windows.Data;

namespace Geowigo.Views
{
	public partial class HomePage : BasePage
	{
		#region Properties

		#region ViewModel

		public new HomeViewModel ViewModel
		{
			get
			{
				return DataContext as HomeViewModel;
			}
		}

		#endregion

		#endregion

        #region Members

        private CollectionViewSource _HistorySource;

        #endregion
		
		public HomePage()
		{
			InitializeComponent();

            _HistorySource = (CollectionViewSource)this.Resources["HistorySource"];
			ViewModel.InitCollectionViewSources(_HistorySource);
		}
	}
}