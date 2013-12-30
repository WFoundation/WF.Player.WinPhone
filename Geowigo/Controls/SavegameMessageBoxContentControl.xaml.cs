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
    public partial class SavegameMessageBoxContentControl : UserControl
    {
        #region Dependency Properties

        #region Savegame


        public CartridgeSavegame Savegame
        {
            get { return (CartridgeSavegame)GetValue(SavegameProperty); }
            set { SetValue(SavegameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Savegame.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SavegameProperty =
            DependencyProperty.Register("Savegame", typeof(CartridgeSavegame), typeof(SavegameMessageBoxContentControl), new PropertyMetadata(null, OnSavegamePropertyChanged));

        private static void OnSavegamePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SavegameMessageBoxContentControl)o).OnSavegameChanged(e.NewValue as CartridgeSavegame);
        }

        #endregion

        #region Name

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(SavegameMessageBoxContentControl), new PropertyMetadata(null));



        #endregion

        #region HashBrush


        public Brush HashBrush
        {
            get { return (Brush)GetValue(HashBrushProperty); }
            set { SetValue(HashBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HashBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HashBrushProperty =
            DependencyProperty.Register("HashBrush", typeof(Brush), typeof(SavegameMessageBoxContentControl), new PropertyMetadata(null));


        #endregion

        #endregion
        
        public SavegameMessageBoxContentControl()
        {
            InitializeComponent();
        }

        private void OnSavegameChanged(CartridgeSavegame cs)
        {
            if (cs == null)
            {
                Name = null;
                HashBrush = null;
            }
            else
            {
                Name = cs.Name;
                HashBrush = GetHashBrush(cs);
            }
        }

        private Brush GetHashBrush(CartridgeSavegame cs)
        {
            // Gets bytes from the hash of the name.
            byte[] bytes = BitConverter.GetBytes(cs.Name.GetHashCode());

            // Computes a color from each byte.
            return new SolidColorBrush(Color.FromArgb(255, bytes[1], bytes[2], bytes[3]));
        }
    }
}
