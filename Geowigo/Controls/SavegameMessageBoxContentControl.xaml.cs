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

        #region SavegameName

        public string SavegameName
        {
            get { return (string)GetValue(SavegameNameProperty); }
            set { SetValue(SavegameNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SavegameNameProperty =
            DependencyProperty.Register("SavegameName", typeof(string), typeof(SavegameMessageBoxContentControl), new PropertyMetadata(null, OnSavegameNameChanged));

        private static void OnSavegameNameChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SavegameMessageBoxContentControl)o).OnSavegameNameChanged(e.NewValue as string);
        }

        #endregion

        #region HashColor



        public Color HashColor
        {
            get { return (Color)GetValue(HashColorProperty); }
            set { SetValue(HashColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HashColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HashColorProperty =
            DependencyProperty.Register("HashColor", typeof(Color), typeof(SavegameMessageBoxContentControl), new PropertyMetadata(default(Color)));


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
                HashColor = default(Color);
            }
            else
            {
                Name = cs.Name;
                HashColor = cs.HashColor;
            }
        }

        private void OnSavegameNameChanged(string newValue)
        {
            HashColor = CartridgeSavegame.GetHashColor(newValue ?? "");
        }
    }
}
