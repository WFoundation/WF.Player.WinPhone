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
using Geowigo.Models;

namespace Geowigo.Controls
{
	public partial class WherigoMessageBoxContentControl : UserControl
	{
		#region Dependency Properties

		#region MessageBox

		public WherigoMessageBox MessageBox
		{
			get { return (WherigoMessageBox)GetValue(MessageBoxProperty); }
			set { SetValue(MessageBoxProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MessageBox.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MessageBoxProperty =
			DependencyProperty.Register("MessageBox", typeof(WherigoMessageBox), typeof(WherigoMessageBoxContentControl), new PropertyMetadata(null));

		#endregion

		#endregion
		
		public WherigoMessageBoxContentControl()
		{
			InitializeComponent();
		}
	}
}
