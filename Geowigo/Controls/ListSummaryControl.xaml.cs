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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;

namespace Geowigo.Controls
{
	/// <summary>
	/// A control that displays a summary of a list of items.
	/// </summary>
	public partial class ListSummaryControl : UserControl
	{
		#region Dependency Properties

		#region Items


		public IEnumerable Items
		{
			get { return (IEnumerable)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(IEnumerable), typeof(ListSummaryControl), new PropertyMetadata(null, OnItemsPropertyChanged));

		private static void OnItemsPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((ListSummaryControl)o).OnItemsPropertyChanged(e);
		}

		#endregion

		#region HeaderFormatText

		public string HeaderFormatText
		{
			get { return (string)GetValue(HeaderFormatTextProperty); }
			set { SetValue(HeaderFormatTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HeaderFormatText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HeaderFormatTextProperty =
			DependencyProperty.Register("HeaderFormatText", typeof(string), typeof(ListSummaryControl), new PropertyMetadata(null, OnHeaderFormatTextPropertyChanged));

		private static void OnHeaderFormatTextPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((ListSummaryControl)o).OnHeaderFormatTextChanged(e);
		}



		#endregion

		#region HeaderPluralString


		public string HeaderPluralString
		{
			get { return (string)GetValue(HeaderPluralStringProperty); }
			set { SetValue(HeaderPluralStringProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HeaderPluralString.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HeaderPluralStringProperty =
			DependencyProperty.Register("HeaderPluralString", typeof(string), typeof(ListSummaryControl), new PropertyMetadata(null, OnHeaderPluralStringPropertyChanged));

		private static void OnHeaderPluralStringPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((ListSummaryControl)o).OnHeaderPluralStringChanged(e);
		}



		#endregion

		#region ListSeparatorString


		public string ListSeparatorString
		{
			get { return (string)GetValue(ListSeparatorStringProperty); }
			set { SetValue(ListSeparatorStringProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ListSeparatorString.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ListSeparatorStringProperty =
			DependencyProperty.Register("ListSeparatorString", typeof(string), typeof(ListSummaryControl), new PropertyMetadata(null, OnListSeparatorStringPropertyChanged));

		private static void OnListSeparatorStringPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((ListSummaryControl)o).OnListSeparatorStringChanged(e);
		}


		#endregion

		#region HeaderStyle


		public Style HeaderStyle
		{
			get { return (Style)GetValue(HeaderStyleProperty); }
			set { SetValue(HeaderStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HeaderStyle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HeaderStyleProperty =
			DependencyProperty.Register("HeaderStyle", typeof(Style), typeof(ListSummaryControl), new PropertyMetadata(null));


		#endregion

		#region ListStyle


		public Style ListStyle
		{
			get { return (Style)GetValue(ListStyleProperty); }
			set { SetValue(ListStyleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ListStyle.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ListStyleProperty =
			DependencyProperty.Register("ListStyle", typeof(Style), typeof(ListSummaryControl), new PropertyMetadata(null));


		#endregion

		#region ItemToStringConverter



		public System.Windows.Data.IValueConverter ItemToStringConverter
		{
			get { return (System.Windows.Data.IValueConverter)GetValue(ItemToStringConverterProperty); }
			set { SetValue(ItemToStringConverterProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemToStringConverter.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ItemToStringConverterProperty =
			DependencyProperty.Register("ItemToStringConverter", typeof(System.Windows.Data.IValueConverter), typeof(ListSummaryControl), new PropertyMetadata(null));



		#endregion

		#region DisablesOnEmpty


		public bool DisablesOnEmpty
		{
			get { return (bool)GetValue(DisablesOnEmptyProperty); }
			set { SetValue(DisablesOnEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DisablesOnEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DisablesOnEmptyProperty =
			DependencyProperty.Register("DisablesOnEmpty", typeof(bool), typeof(ListSummaryControl), new PropertyMetadata(true));


		#endregion

		#endregion

		#region Fields
		private bool _isInitialized = false;
		private bool _hasBufferForIsEnabled = false;
		private bool _bufferedIsEnabled = false;

		private Brush normalBrush;
		private Brush emptyBrush;
		#endregion

		public ListSummaryControl()
		{
			HeaderPluralString = "S";
			ListSeparatorString = ", ";
			HeaderFormatText = "{0} ITEM{1}";

			Loaded += new RoutedEventHandler(OnLoaded);
			
			InitializeComponent();

			Refresh();
		}


		protected void OnItemsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			INotifyCollectionChanged oldValue = e.OldValue as INotifyCollectionChanged;
			INotifyCollectionChanged newValue = e.NewValue as INotifyCollectionChanged;
			
			if (oldValue != null)
			{
				oldValue.CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsCollectionChanged);
			}

			if (newValue != null)
			{
				newValue.CollectionChanged += new NotifyCollectionChangedEventHandler(OnItemsCollectionChanged);
			}

			Refresh();
		}

		protected void OnHeaderFormatTextChanged(DependencyPropertyChangedEventArgs e)
		{
			RefreshHeader();
		}

		protected void OnHeaderPluralStringChanged(DependencyPropertyChangedEventArgs e)
		{
			RefreshHeader();
		}

		protected void OnListSeparatorStringChanged(DependencyPropertyChangedEventArgs e)
		{
			RefreshList();
		}

		private void OnItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			Refresh();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			normalBrush = (Brush)Resources["PhoneForegroundBrush"];
			emptyBrush = (Brush)Resources["PhoneDisabledBrush"];
			
			_isInitialized = true;

			RefreshVisualState(false);
		}

		private void Refresh()
		{
			// Refreshes the header.
			RefreshHeader();

			// Refreshes the list.
			RefreshList();

			// Refreshes the visual state.
			RefreshVisualState();
		}

		private void RefreshVisualState(bool forceRefresh = true)
		{
			if (!_hasBufferForIsEnabled || forceRefresh)
			{
				_bufferedIsEnabled = !DisablesOnEmpty || (Items != null && Items.OfType<object>().Count() > 0); 
			}

			if (!_isInitialized)
			{
				_hasBufferForIsEnabled = true;
				return;
			}
			
			_hasBufferForIsEnabled = false;
			HeaderTextBlock.Foreground = _bufferedIsEnabled ? normalBrush : emptyBrush;
			ListTextBlock.Foreground = _bufferedIsEnabled ? normalBrush : emptyBrush;
		}

		private void RefreshList()
		{
			if (ListTextBlock == null)
			{
				return;
			}

			ListTextBlock.Inlines.Clear();

			if (Items == null)
			{
				return;
			}

			int toDo = Items.Cast<object>().Count();
			int doneSoFar = 0;
			foreach (var item in Items)
			{
				doneSoFar++;

				ListTextBlock.Inlines.Add(new Run() { Text = GetString(item) });

				if (doneSoFar < toDo)
				{
					ListTextBlock.Inlines.Add(new Run() { Text = ListSeparatorString ?? ", " });
				}
			}
		}

		private string GetString(object item)
		{
			string target = null;

			if (ItemToStringConverter != null)
			{
				target = ItemToStringConverter.Convert(item, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture) as string;
			}

			if (target == null)
			{
				target = item.ToString();
			}

			return target;
		}

		private void RefreshHeader()
		{
			if (HeaderTextBlock == null)
			{
				return;
			}

			int itemCount = Items != null ? Items.Cast<object>().Count() : 0;

			HeaderTextBlock.Text = String.Format(HeaderFormatText ?? "{0} ITEM{1}", itemCount, itemCount > 1 ? (HeaderPluralString ?? "S") : "");
		}
	}
}
