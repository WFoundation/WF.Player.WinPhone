using Geowigo.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Geowigo.Utils;
using System.ComponentModel;
using Geowigo.Models.Providers;
using System.IO.IsolatedStorage;
using Windows.ApplicationModel.Email;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.Phone.Tasks;
using Geowigo.Models;

namespace Geowigo.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region DPs

        #region AreAdvancedSettingsDisplayed


        public bool AreAdvancedSettingsDisplayed
        {
            get { return (bool)GetValue(AreAdvancedSettingsDisplayedProperty); }
            set { SetValue(AreAdvancedSettingsDisplayedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AreAdvancedSettingsDisplayed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AreAdvancedSettingsDisplayedProperty =
            DependencyProperty.Register("AreAdvancedSettingsDisplayed", typeof(bool), typeof(SettingsViewModel), new PropertyMetadata(false));


        #endregion

        #region LogFileCount


        public int LogFileCount
        {
            get { return (int)GetValue(LogFileCountProperty); }
            set { SetValue(LogFileCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LogFileCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LogFileCountProperty =
            DependencyProperty.Register("LogFileCount", typeof(int), typeof(SettingsViewModel), new PropertyMetadata(0));


        #endregion

        #region IsOneDriveProviderEnabled


        public bool IsOneDriveProviderEnabled
        {
            get { return (bool)GetValue(IsOneDriveProviderEnabledProperty); }
            set { SetValue(IsOneDriveProviderEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOneDriveProviderEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOneDriveProviderEnabledProperty =
            DependencyProperty.Register("IsOneDriveProviderEnabled", typeof(bool), typeof(SettingsViewModel), new PropertyMetadata(false, OnIsOneDriveProviderEnabledPropertyChanged));

        private static void OnIsOneDriveProviderEnabledPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SettingsViewModel)o).OnIsOneDriveProviderEnabledChanged((bool)e.NewValue);
        }

        #endregion

        #region OneDriveProviderSimpleStatus


        public string OneDriveProviderSimpleStatus
        {
            get { return (string)GetValue(OneDriveProviderSimpleStatusProperty); }
            set { SetValue(OneDriveProviderSimpleStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OneDriveProviderSimpleStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OneDriveProviderSimpleStatusProperty =
            DependencyProperty.Register("OneDriveProviderSimpleStatus", typeof(string), typeof(SettingsViewModel), new PropertyMetadata(null));


        #endregion

        #region OneDriveProviderAdvancedStatus


        public string OneDriveProviderAdvancedStatus
        {
            get { return (string)GetValue(OneDriveProviderAdvancedStatusProperty); }
            set { SetValue(OneDriveProviderAdvancedStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OneDriveProviderAdvancedStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OneDriveProviderAdvancedStatusProperty =
            DependencyProperty.Register("OneDriveProviderAdvancedStatus", typeof(string), typeof(SettingsViewModel), new PropertyMetadata(null));


        #endregion

        #region CustomSupportStatus


        public string CustomSupportStatus
        {
            get { return (string)GetValue(CustomSupportStatusProperty); }
            set { SetValue(CustomSupportStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomSupportStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomSupportStatusProperty =
            DependencyProperty.Register("CustomSupportStatus", typeof(string), typeof(SettingsViewModel), new PropertyMetadata(null));


        #endregion

        #region CartridgeLogFileCount


        public int CartridgeLogFileCount
        {
            get { return (int)GetValue(CartridgeLogFileCountProperty); }
            set { SetValue(CartridgeLogFileCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CartridgeLogFileCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CartridgeLogFileCountProperty =
            DependencyProperty.Register("CartridgeLogFileCount", typeof(int), typeof(SettingsViewModel), new PropertyMetadata(0));


        #endregion

        #endregion

        #region Properties

        #region AppVersion
        public string AppVersion
        {
            get
            {
                return App.Current.ViewModel.AppVersion;
            }
        }
        #endregion

        #endregion

        #region Commands

        #region DisplayAdvancedSettingsCommand
        private RelayCommand _DisplayAdvancedSettingsCommand;

        public RelayCommand DisplayAdvancedSettingsCommand
        {
            get
            {
                if (_DisplayAdvancedSettingsCommand == null)
                {
                    _DisplayAdvancedSettingsCommand = new RelayCommand(DisplayAdvancedSettings);
                }

                return _DisplayAdvancedSettingsCommand;
            }
        }
        #endregion

        #region NavigateToDeviceInfoCommand
        private RelayCommand _NavigateToDeviceInfoCommand;

        public RelayCommand NavigateToDeviceInfoCommand
        {
            get
            {
                if (_NavigateToDeviceInfoCommand == null)
                {
                    _NavigateToDeviceInfoCommand = new RelayCommand(NavigateToDeviceInfo);
                }

                return _NavigateToDeviceInfoCommand;
            }
        }
        #endregion

        #region ClearCartridgeCacheCommand
        private RelayCommand _ClearCartridgeCacheCommand;

        public RelayCommand ClearCartridgeCacheCommand
        {
            get
            {
                if (_ClearCartridgeCacheCommand == null)
                {
                    _ClearCartridgeCacheCommand = new RelayCommand(ClearCartridgeCache);
                }

                return _ClearCartridgeCacheCommand;
            }
        }
        #endregion

        #region GetSupportCommand
        private RelayCommand _GetSupportCommand;

        public RelayCommand GetSupportCommand
        {
            get
            {
                if (_GetSupportCommand == null)
                {
                    _GetSupportCommand = new RelayCommand(NavigateToGetSupport);
                }

                return _GetSupportCommand;
            }
        }
        #endregion

        #region ClearDebugReportCommand
        private RelayCommand _ClearDebugReportCommand;

        public RelayCommand ClearDebugReportCommand
        {
            get
            {
                if (_ClearDebugReportCommand == null)
                {
                    _ClearDebugReportCommand = new RelayCommand(ClearDebugReport, CanDebugReportCommandExecute);
                }

                return _ClearDebugReportCommand;
            }
        }
        #endregion

        #region ClearHistoryCommand
        private RelayCommand _ClearHistoryCommand;

        public RelayCommand ClearHistoryCommand
        {
            get
            {
                if (_ClearHistoryCommand == null)
                {
                    _ClearHistoryCommand = new RelayCommand(ClearHistory);
                }

                return _ClearHistoryCommand;
            }
        }
        #endregion

        #region NavigateToPrivacyPolicyCommand
        private RelayCommand _NavigateToPrivacyPolicyCommand;

        public RelayCommand NavigateToPrivacyPolicyCommand
        {
            get
            {
                if (_NavigateToPrivacyPolicyCommand == null)
                {
                    _NavigateToPrivacyPolicyCommand = new RelayCommand(NavigateToPrivacyPolicy);
                }

                return _NavigateToPrivacyPolicyCommand;
            }
        }
        #endregion

        #region DeleteCartridgeLogsCommand
        private RelayCommand _DeleteCartridgeLogsCommand;

        public RelayCommand DeleteCartridgeLogsCommand
        {
            get
            {
                if (_DeleteCartridgeLogsCommand == null)
                {
                    _DeleteCartridgeLogsCommand = new RelayCommand(DeleteCartridgeLogs, CanDeleteCartridgeLogsCommandExecute);
                }

                return _DeleteCartridgeLogsCommand;
            }
        }
        #endregion

        #endregion

        #region Constants
        public static readonly string ProviderServiceNameKey = "providerServiceName";
        public static readonly string ProviderWizardKey = "providerLinkWizard";
        #endregion

        #region Fields

        private BackgroundWorker _clearCacheWorker;
        private bool _isReady;
        private ProgressAggregator _progress;
        private Models.Settings _appSettings;

        #endregion

        public SettingsViewModel()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            _progress = new ProgressAggregator();
            _progress.PropertyChanged += OnProgressAggregatorPropertyChanged;
            
            _clearCacheWorker = new BackgroundWorker();
            _clearCacheWorker.DoWork += ClearCartridgeCacheCore;

            _appSettings = App.Current.Model.Settings;
            _appSettings.PropertyChanged += OnAppSettingsPropertyChanged;

            Model.CartridgeStore.PropertyChanged += OnCartridgeStorePropertyChanged;
        }
 
        #region Command Actions

        private void ClearDebugReport()
        {
            DebugUtils.ClearCache();

            RefreshLogView();
        }

        private void NavigateToGetSupport()
        {
            App.Current.ViewModel.NavigationManager.NavigateToHelp();
        }

        private bool CanDebugReportCommandExecute()
        {
            return DebugUtils.GetDebugReportFileCount() > 0;
        }

        private async void DisplayAdvancedSettings()
        {
            // Fetches some potentially expansive data now.
            LicensingManager licensingManager = App.Current.ViewModel.LicensingManager;
            CustomSupportStatus = "Custom Support IAP: "
                + (await licensingManager.ValidateCustomSupportLicense() ? "Yes" : "No")
                + (licensingManager.HasCustomSupportCertificate ? ", Installed" : "");
            
            // Displays!
            AreAdvancedSettingsDisplayed = true;
        }

        private void NavigateToDeviceInfo()
        {
            App.Current.ViewModel.NavigationManager.NavigateToPlayerInfo();
        }

        private void NavigateToPrivacyPolicy()
        {
            WebBrowserTask task = new WebBrowserTask()
            {
                Uri = new Uri("http://mangatome.net/tools/geowigo/privacy.html", UriKind.Absolute)
            };
            task.Show();
        }

        private void ClearCartridgeCache()
        {
            if (_clearCacheWorker.IsBusy)
            {
                return;
            }

            _clearCacheWorker.RunWorkerAsync();
        }

        private void ClearCartridgeCacheCore(object sender, DoWorkEventArgs e)
        {
            // Clears the cache.
            AppViewModel vm = App.Current.ViewModel;
            vm.Model.CartridgeStore.ClearCache();

            // Syncs all again.
            vm.Model.CartridgeStore.SyncFromIsoStore();
        }

        private void ClearHistory()
        {
            // Asks for clearing the history.
            if (System.Windows.MessageBox.Show("Do you want to delete all entries of the history?", "Clear history", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
            {
                App.Current.ViewModel.ClearHistory();
            }
        }

        private void DeleteCartridgeLogs()
        {
            // Deletes all logs.
            foreach (CartridgeTag tag in Model.CartridgeStore)
            {
                tag.RemoveAllLogs();
            }

            // Refreshes the view.
            RefreshCartridgeLogView();
        }

        private bool CanDeleteCartridgeLogsCommandExecute()
        {
            return CartridgeLogFileCount > 0;
        }

        #endregion

        protected override void InitFromNavigation(BaseViewModel.NavigationInfo nav)
        {
            RefreshAll();

            if (nav.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
            {
                // Starts linking the OneDrive provider if needed.
                string providerServiceName = nav.GetQueryValueOrDefault(ProviderServiceNameKey);
                if (nav.GetQueryValueOrDefault(ProviderWizardKey) == Boolean.TrueString && providerServiceName != null)
                {
                    // A wizard should be performed if needed.

                    if (!IsOneDriveProviderEnabled && GetOneDriveProvider().ServiceName == providerServiceName)
                    {
                        // A wizard is needed.
                        RunOneDriveProviderLinkWizard();
                    }
                } 
            }
        }

        private void RefreshAll()
        {
            RefreshCartridgeStore();

            RefreshLogView();

            RefreshCartridgeLogView();

            RefreshOneDrive();
        }

        private OneDriveCartridgeProvider GetOneDriveProvider()
        {
            return Model.CartridgeStore.Providers.OfType<OneDriveCartridgeProvider>().FirstOrDefault();
        }

        private void RefreshOneDrive()
        {
            // Refresh provider info.
            OneDriveCartridgeProvider provider = GetOneDriveProvider();
            if (provider == null)
            {
                // We're sure the provider is not linked.
                _appSettings.ProviderLinkedHint = false;
                IsOneDriveProviderEnabled = false;
            }
            else if (provider.IsLinked)
            {
                // We know the provider is linked, keep this memory.
                _appSettings.ProviderLinkedHint = true;
                IsOneDriveProviderEnabled = true;
            }
            else
            {
                // We're not sure if the provider is unlinked, or if the internet is just off.
                // Use previously stored data to figure it out.
                IsOneDriveProviderEnabled = _appSettings.ProviderLinkedHint;
            }

            // Refresh simple status.
            string simpleStatus = null;
            if (provider != null && (provider.IsLinked || provider.CartridgeCount > 0))
            {
                simpleStatus = "";
                
                if (provider.IsLinked && !String.IsNullOrEmpty(provider.OwnerName))
                {
                    simpleStatus += provider.OwnerName + ", ";
                }
                
                simpleStatus += String.Format("{0} cartridges", provider.CartridgeCount);
            }
            OneDriveProviderSimpleStatus = simpleStatus;

            // Refresh advanced status.
            string advancedStatus = null;
            if (provider != null)
            {
                if (provider.IsLinked || provider.IsSyncing)
                {
                    // Provider is linked: sync status.
                    
                    if (provider.IsSyncing)
                    {
                        advancedStatus = "Syncing...";
                    }
                    else
                    {
                        advancedStatus = "Synchronized. ";

                        advancedStatus += "Downloads from OneDrive folder /Geowigo. ";

                        if (_appSettings.CanProviderUpload)
                        {
                            advancedStatus += "Uploads to OneDrive folder /Geowigo/Uploads. ";
                        }
                        else
                        {
                            advancedStatus += "Upload disabled. ";
                        }
                    }
                }
                else if (provider.CartridgeCount > 0)
                {
                    // Provider not linked but local content exists.

                    if (_appSettings.ProviderLinkedHint)
                    {
                        advancedStatus = "Cartridges and savegames will not be synchronized with the cloud until the device can reach the Internet.";
                    }
                    else
                    {
                        advancedStatus = "Cartridges and savegames are not synchronized with the cloud unless you link your OneDrive account.";
                    }
                }
            }
            OneDriveProviderAdvancedStatus = advancedStatus;
        }

        private void RefreshLogView()
        {
            LogFileCount = DebugUtils.GetDebugReportFileCount();
            ClearDebugReportCommand.RefreshCanExecute();
        }

        private void RefreshCartridgeLogView()
        {
            CartridgeLogFileCount = Model.CartridgeStore.Aggregate<CartridgeTag, int>(0, (i, ct) => i + ct.LogFiles.Count());
            DeleteCartridgeLogsCommand.RefreshCanExecute();
        }

        private void RefreshCartridgeStore()
        {
            _progress["CartridgeStore"] = Model.CartridgeStore.IsBusy;
        }

        private void RefreshProgressBar(bool isVisible, string message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsProgressBarVisible = isVisible;
                ProgressBarStatusText = message;
            });
        }

        private void OnAppSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CanProviderUpload")
            {
                RefreshOneDrive();
            }
        }

        private void OnCartridgeStorePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsBusy")
            {
                if (Model.CartridgeStore.IsBusy)
                {
                    RefreshCartridgeStore();
                }
                else
                {
                    RefreshAll();
                }
            }
        }

        private void OnIsOneDriveProviderEnabledChanged(bool newValue)
        {
            if (!_isReady)
            {
                return;
            }
            
            OneDriveCartridgeProvider provider = GetOneDriveProvider();

            if (!newValue && _appSettings.ProviderLinkedHint)
            {
                // Coerce value.
                if (MessageBox.Show("Geowigo will forget the link to your OneDrive account. Cartridges and savegames will be kept and playable until you link to OneDrive again.", "Unlink OneDrive", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    // Unlink.
                    try
                    {
                        provider.Unlink();
                        _appSettings.ProviderLinkedHint = false;
                    }
                    catch (Exception e)
                    {
                        DebugUtils.DumpException(e, "OneDrive unlink", true);
                        MessageBox.Show("An error occurred while trying to unlink your OneDrive account. Make sure the device can reach the internet.", "Error", MessageBoxButton.OK);
                        _appSettings.ProviderLinkedHint = true;
                    }
                }
                else
                {
                    // Restores the previous state.
                    IsOneDriveProviderEnabled = _appSettings.ProviderLinkedHint;
                }
            }
            else if (newValue && !_appSettings.ProviderLinkedHint)
            {
                // Starts linking.
                RunOneDriveProviderLinkWizard();
            }

            RefreshOneDrive();
        }

        private void RunOneDriveProviderLinkWizard()
        {
            OneDriveCartridgeProvider provider = GetOneDriveProvider();
            
            // Coerce value.
            if (provider.CartridgeCount == 0 ||
                MessageBox.Show("Cartridges and savegames from a previous OneDrive link are still stored locally. They may be deleted, depending on the OneDrive account you are about to link to.", "Overwriting Link", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // Starts the link.
                provider.BeginLink();
            }
            else
            {
                // Restores the previous state.
                IsOneDriveProviderEnabled = _appSettings.ProviderLinkedHint;
            }
        }

        internal void OnPageReady()
        {
            // The page is now ready and the user is interacting.
            _isReady = true;
        }

        private void OnProgressAggregatorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FirstWorkingSource")
            {
                string source = _progress.FirstWorkingSource as string;
                if (source == "CartridgeStore")
                {
                    RefreshProgressBar(true, "Preparing cartridges...");
                }
                else if (source == null)
                {
                    RefreshProgressBar(false, null);
                }
            }
        }
    }
}
