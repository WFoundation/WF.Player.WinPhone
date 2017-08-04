using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using Geowigo.Utils;
using System.ComponentModel;
using WF.Player.Core;

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
        private static bool SyncOnStartUpSettingDefaultValue = true;

        /// <summary>
        /// Gets or sets if cartridge providers sync automatically on start-up.
        /// </summary>
        public bool SyncOnStartUp
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(SyncOnStartUpSettingKey, SyncOnStartUpSettingDefaultValue);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(SyncOnStartUpSettingKey, SyncOnStartUpSettingDefaultValue);
                
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
        private static bool ProviderLinkedHintSettingDefaultValue = false;

        /// <summary>
        /// Gets or sets if the provider is known to be linked.
        /// </summary>
        public bool ProviderLinkedHint
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(ProviderLinkedHintSettingKey, ProviderLinkedHintSettingDefaultValue);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(ProviderLinkedHintSettingKey, ProviderLinkedHintSettingDefaultValue);

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
        private static bool CanProviderUploadSettingDefaultValue = true;

        /// <summary>
        /// Gets or sets if the provider can upload.
        /// </summary>
        public bool CanProviderUpload
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(CanProviderUploadSettingKey, CanProviderUploadSettingDefaultValue);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(CanProviderUploadSettingKey, CanProviderUploadSettingDefaultValue);

                _settings.SetValueAndSave(CanProviderUploadSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("CanProviderUpload");
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

        #region CanGenerateCartridgeLog
        private static string CanGenerateCartridgeLogSettingKey = "Engine.CanGenerateCartridgeLog";
        private static bool CanGenerateCartridgeLogSettingDefaultValue = true;

        /// <summary>
        /// Gets or sets if the game engine can generate cartridge logs (GWL).
        /// </summary>
        public bool CanGenerateCartridgeLog
        {
            get
            {
                return _settings.GetValueOrDefault<bool>(CanGenerateCartridgeLogSettingKey, CanGenerateCartridgeLogSettingDefaultValue);
            }

            set
            {
                bool changed = value != _settings.GetValueOrDefault<bool>(CanGenerateCartridgeLogSettingKey, CanGenerateCartridgeLogSettingDefaultValue);

                _settings.SetValueAndSave(CanGenerateCartridgeLogSettingKey, value);

                if (changed)
                {
                    RaisePropertyChanged("CanGenerateCartridgeLog");
                }
            }
        }
        #endregion

		#region LengthUnit
		private static string LengthUnitSettingKey = "Engine.LengthUnit";
		private static DistanceUnit LengthUnitSettingDefaultValue = DistanceUnit.Meters;

		/// <summary>
		/// Gets or sets the default length unit for displayed distances.
		/// </summary>
		public DistanceUnit LengthUnit
		{
			get
			{
				return _settings.GetValueOrDefault<DistanceUnit>(LengthUnitSettingKey, LengthUnitSettingDefaultValue);
			}

			set
			{
				bool changed = value != _settings.GetValueOrDefault<DistanceUnit>(LengthUnitSettingKey, LengthUnitSettingDefaultValue);

				_settings.SetValueAndSave(LengthUnitSettingKey, value);

				if (changed)
				{
					RaisePropertyChanged("LengthUnit");
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
