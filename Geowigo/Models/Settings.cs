﻿using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using Geowigo.Utils;
using System.ComponentModel;

namespace Geowigo.Models
{
    public class Settings : INotifyPropertyChanged
    {
        #region Members

        private IsolatedStorageSettings _settings;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        #region SyncOnStartUp

        private static string SyncOnStartUpSettingKey = "OneDrive.SyncOnStartUp";

        /// <summary>
        /// Gets or sets if cartridge providers sync automatically on start-up.
        /// </summary>
        public bool SyncOnStartUp
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(SyncOnStartUpSettingKey, true);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(SyncOnStartUpSettingKey, true);
                
                _settings.SetValueAndSave(SyncOnStartUpSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("SyncOnStartUp");
                }
            }
        }

        #endregion

        #region ProviderLinkedHint

        private static string ProviderLinkedHintSettingKey = "OneDrive.LinkHint";

        /// <summary>
        /// Gets or sets if the provider is known to be linked.
        /// </summary>
        public bool ProviderLinkedHint
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(ProviderLinkedHintSettingKey);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(ProviderLinkedHintSettingKey);

                _settings.SetValueAndSave(ProviderLinkedHintSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("ProviderLinkedHint");
                }
            }
        }

        #endregion

        #region CanProviderUpload

        private static string CanProviderUploadSettingKey = "OneDrive.CanUpload";

        /// <summary>
        /// Gets or sets if the provider can upload.
        /// </summary>
        public bool CanProviderUpload
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(CanProviderUploadSettingKey, true);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(CanProviderUploadSettingKey, true);

                _settings.SetValueAndSave(CanProviderUploadSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("CanProviderUpload");
                }
            }
        }

        #endregion

        #region IgnoreObsoleteVersionWarning

        private static string IgnoreObsoleteVersionWarningSettingKey = "Beta.IgnoreObsoleteVersionWarning";

        /// <summary>
        /// Gets or sets if a warning about the app version being obsolete should be displayed.
        /// </summary>
        public bool IgnoreObsoleteVersionWarning
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(IgnoreObsoleteVersionWarningSettingKey);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(IgnoreObsoleteVersionWarningSettingKey);

                _settings.SetValueAndSave(IgnoreObsoleteVersionWarningSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("IgnoreObsoleteVersionWarning");
                }
            }
        }

        #endregion

        #region MapCartographicMode
        private static string MapCartographicModeSettingKey = "Map.CartographicMode";

        /// <summary>
        /// Gets or sets the preferred cartographic mode of the map.
        /// </summary>
        public Microsoft.Phone.Maps.Controls.MapCartographicMode MapCartographicMode
        {
            get
            {
                return _settings.GetValueOrDefault<Microsoft.Phone.Maps.Controls.MapCartographicMode>(MapCartographicModeSettingKey, Microsoft.Phone.Maps.Controls.MapCartographicMode.Hybrid);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<Microsoft.Phone.Maps.Controls.MapCartographicMode>(MapCartographicModeSettingKey, Microsoft.Phone.Maps.Controls.MapCartographicMode.Hybrid);

                _settings.SetValueAndSave(MapCartographicModeSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("MapCartographicMode");
                }
            }
        }
        #endregion

        #endregion

        public Settings()
        {
            _settings = IsolatedStorageSettings.ApplicationSettings;
        }

        private void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
