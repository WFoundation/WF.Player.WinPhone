using Geowigo.Controls;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Geowigo.Utils;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Windows.Storage;
using Windows.ApplicationModel.Email;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Store;

namespace Geowigo.ViewModels
{
    public class HelpViewModel : BaseViewModel, INotifyPropertyChanged
    {
        #region Nested Classes

        public enum Mode
        {
            Menu,
            BugReport
        }

        #endregion

        #region Dependency Properties

        #region IsAppSupporterContentVisible

        public bool IsAppSupporterContentVisible
        {
            get { return (bool)GetValue(IsAppSupporterContentVisibleProperty); }
            set { SetValue(IsAppSupporterContentVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAppSupporterContentVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAppSupporterContentVisibleProperty =
            DependencyProperty.Register("IsAppSupporterContentVisible", typeof(bool), typeof(HelpViewModel), new PropertyMetadata(false));


        #endregion

        #region ReportProblemListItemSource


        public IList<string> ReportProblemListItemSource
        {
            get { return (IList<string>)GetValue(ReportProblemListItemSourceProperty); }
            set { SetValue(ReportProblemListItemSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportProblemListItemSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportProblemListItemSourceProperty =
            DependencyProperty.Register("ReportProblemListItemSource", typeof(IList<string>), typeof(HelpViewModel), new PropertyMetadata(null));


        #endregion

        #region ReportProblemListSelectedIndex


        public int ReportProblemListSelectedIndex
        {
            get { return (int)GetValue(ReportProblemListSelectedIndexProperty); }
            set { SetValue(ReportProblemListSelectedIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportProblemListSelectedIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportProblemListSelectedIndexProperty =
            DependencyProperty.Register("ReportProblemListSelectedIndex", typeof(int), typeof(HelpViewModel), new PropertyMetadata(0));


        #endregion

        #region ReportLocationListItemSource


        public IList<string> ReportLocationListItemSource
        {
            get { return (IList<string>)GetValue(ReportLocationListItemSourceProperty); }
            set { SetValue(ReportLocationListItemSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportLocationListItemSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportLocationListItemSourceProperty =
            DependencyProperty.Register("ReportLocationListItemSource", typeof(IList<string>), typeof(HelpViewModel), new PropertyMetadata(null));


        #endregion

        #region ReportLocationListSelectedIndex


        public int ReportLocationListSelectedIndex
        {
            get { return (int)GetValue(ReportLocationListSelectedIndexProperty); }
            set { SetValue(ReportLocationListSelectedIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportLocationListSelectedIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportLocationListSelectedIndexProperty =
            DependencyProperty.Register("ReportLocationListSelectedIndex", typeof(int), typeof(HelpViewModel), new PropertyMetadata(0));


        #endregion

        #region ReportExtraNotes


        public string ReportExtraNotes
        {
            get { return (string)GetValue(ReportExtraNotesProperty); }
            set { SetValue(ReportExtraNotesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportExtraNotes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportExtraNotesProperty =
            DependencyProperty.Register("ReportExtraNotes", typeof(string), typeof(HelpViewModel), new PropertyMetadata(null));


        #endregion

        #endregion

        #region Properties

        #region DisplayMode
        public Mode DisplayMode
        {
            get
            {
                return _DisplayMode;
            }

            private set
            {
                bool valueChanged = value != _DisplayMode;

                _DisplayMode = value;

                if (valueChanged)
                {
                    RaisePropertyChanged("DisplayMode");

                    OnDisplayModeChanged();
                }
            }
        }

        private Mode _DisplayMode;
        #endregion

        #endregion

        #region Commands

        #region NavigateToForumThreadCommand
        private ICommand _NavigateToForumThreadCommand;

        public ICommand NavigateToForumThreadCommand
        {
            get
            {
                if (_NavigateToForumThreadCommand == null)
                {
                    _NavigateToForumThreadCommand = new RelayCommand(GoToForumThread);
                }

                return _NavigateToForumThreadCommand;
            }
        }
        #endregion

        #region ShowBugReportWizardCommand
        private ICommand _ShowBugReportWizardCommand;

        public ICommand ShowBugReportWizardCommand
        {
            get
            {
                if (_ShowBugReportWizardCommand == null)
                {
                    _ShowBugReportWizardCommand = new RelayCommand(ShowBugReportWizard);
                }

                return _ShowBugReportWizardCommand;
            }
        }
        #endregion

        #region SendBugReportCommand
        private ICommand _SendBugReportCommand;

        public ICommand SendBugReportCommand
        {
            get
            {
                if (_SendBugReportCommand == null)
                {
                    _SendBugReportCommand = new RelayCommand(SendBugReport);
                }

                return _SendBugReportCommand;
            }
        }
        #endregion

        #region CancelBugReportCommand
        private ICommand _CancelBugReportCommand;

        public ICommand CancelBugReportCommand
        {
            get
            {
                if (_CancelBugReportCommand == null)
                {
                    _CancelBugReportCommand = new RelayCommand(CancelBugReport);
                }

                return _CancelBugReportCommand;
            }
        }
        #endregion

        #region PurchaseSupportCommand
        private ICommand _PurchaseSupportCommand;

        public ICommand PurchaseSupportCommand
        {
            get
            {
                if (_PurchaseSupportCommand == null)
                {
                    _PurchaseSupportCommand = new RelayCommand(RequestPurchaseSupport);
                }

                return _PurchaseSupportCommand;
            }
        }
        #endregion

        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raised when the view model needs the report form to be flushed, and its
        /// bound DPs to be updated.
        /// </summary>
        public event EventHandler ReportFormFlushRequested;
        #endregion

        public HelpViewModel()
        {
            ReportProblemListItemSource = new List<string>() { 
                "Select...",
                "Crash",
                "Nothing happens",
                "What should I do?",
                "Other (explain below)"
            };

            ReportLocationListItemSource = new List<string>() {
                "Select...",
                "OneDrive sync",
                "Playing a cartridge",
                "Other (explain below)"
            };
        }

        protected override async void InitFromNavigation(NavigationInfo nav)
        {
            IsAppSupporterContentVisible = await App.Current.ViewModel.LicensingManager.ValidateCustomSupportLicense();
        }

        protected override void OnPageBackKeyPressOverride(CancelEventArgs e)
        {
            // If we're in Menu mode, lets the even happen. Otherwise, comes back to menu mode
            // and cancels the event.
            if (DisplayMode != Mode.Menu)
            {
                e.Cancel = true;
                DisplayMode = Mode.Menu;
            }
        }

        private void OnDisplayModeChanged()
        {
            Mode mode = DisplayMode;

            if (mode == Mode.Menu)
            {
                // Clears the app bar.
                ApplicationBar = null;
            }
            else if (mode == Mode.BugReport)
            {
                // Makes the bug report app bar.
                ApplicationBar = new ApplicationBar();
                ApplicationBar.CreateAndAddButton("appbar.email.png", SendBugReportCommand, "send");
                ApplicationBar.CreateAndAddButton("appbar.cancel.png", CancelBugReportCommand, "cancel");
            }
        }

        #region Menu Mode
        private void GoToForumThread()
        {
            // Navigates to the forum thread.
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://forums.groundspeak.com/GC/index.php?showtopic=315741", UriKind.Absolute);
            task.Show();
        } 
        #endregion

        #region Bug Report Mode
        private void ShowBugReportWizard()
        {
            // Displays the bug report.
            DisplayMode = Mode.BugReport;
        }

        private async void SendBugReport()
        {
            // Requests a report flush.
            if (ReportFormFlushRequested != null)
            {
                ReportFormFlushRequested(this, EventArgs.Empty);
            }

            // Gets the licensing manager.
            LicensingManager licensingManager = App.Current.ViewModel.LicensingManager;
            
            // Bakes the report.
            DebugUtils.BugReport report = DebugUtils.MakeDebugReport();

            // Maintains local storage copies of the report, so that the latest generated, non-null
            // report is in "report.txt" and the previous "report.txt" is saved as "report.old.txt".
            string filenameNewReport = "report.txt";
            string filenameOldReport = "report.old.txt";
            bool hasNewReport = false;
            bool hasOldReport = false;
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Determines what file operations to perform.
                bool newReportExists = isf.FileExists(filenameNewReport);
                bool shouldWriteReport = report.FileCount > 0 || !newReportExists;
                bool shouldSaveOldReport = newReportExists && shouldWriteReport;
                
                // Saves the old report if needed.
                if (shouldSaveOldReport)
                {
                    try
                    {
                        isf.CopyFile(filenameNewReport, filenameOldReport, true);
                    }
                    catch (Exception)
                    {
                        // Nothing to do, too bad.
                    }
                }
                hasOldReport = isf.FileExists(filenameOldReport);

                // Writes the new report.
                if (shouldWriteReport)
                {
                    try
                    {
                        using (IsolatedStorageFileStream fs = isf.OpenFile(filenameNewReport, System.IO.FileMode.Create))
                        {
                            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fs))
                            {
                                sw.WriteLine(report);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // This is really bad. Let the user know.
                        MessageBox.Show("An error occured during the collection of helpful data. Without this, your problem may be harder to solve. Please make sure to describe your problem the best you can in the e-mail that is about to be sent.", "Warning", MessageBoxButton.OK);
                    }
                }
                hasNewReport = isf.FileExists(filenameNewReport);
            }

            // Gets the report files.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile newReportFile = hasNewReport ? await localFolder.GetFileAsync(filenameNewReport) : null;
            StorageFile oldReportFile = hasOldReport ? await localFolder.GetFileAsync(filenameOldReport) : null;
            StorageFile customSupportFile = licensingManager.HasCustomSupportCertificate ? await localFolder.GetFileAsync(LicensingManager.CustomSupportLicenseFilepath) : null;

            // Makes the body of the e-mail.
            StringBuilder bodySb = new StringBuilder();
            bodySb.AppendLine("An error report is attached with this e-mail.");
            bodySb.AppendLine("");
            if (ReportProblemListSelectedIndex >= 0)
            {
                bodySb.AppendLine("Problem: " + ReportProblemListItemSource[ReportProblemListSelectedIndex]); 
            }
            if (ReportLocationListSelectedIndex >= 0)
            {
                bodySb.AppendLine("Where: " + ReportLocationListItemSource[ReportLocationListSelectedIndex]);
            }
            if (IsAppSupporterContentVisible)
            {
                bodySb.AppendLine("Custom support enabled for user.");
            }
            bodySb.AppendLine("Additional notes:");
            bodySb.AppendLine(ReportExtraNotes);
            bodySb.AppendLine();

            // Makes an e-mail.
            EmailMessage email = new EmailMessage()
            {
                Subject = "Geowigo Bug Report",
                Body = bodySb.ToString()
            };
            email.To.Add(new EmailRecipient("contact@cybisoft.net"));
            if (newReportFile != null)
            {
                email.Attachments.Add(new EmailAttachment(filenameNewReport, RandomAccessStreamReference.CreateFromFile(newReportFile))); 
            }
            if (oldReportFile != null)
            {
                email.Attachments.Add(new EmailAttachment(filenameOldReport, RandomAccessStreamReference.CreateFromFile(oldReportFile))); 
            }
            if (customSupportFile != null)
            {
                email.Attachments.Add(new EmailAttachment(
                    System.IO.Path.GetFileName(LicensingManager.CustomSupportLicenseFilepath), 
                    RandomAccessStreamReference.CreateFromFile(customSupportFile))); 
            }
            // add iap

            // Prompts the user to send the e-mail.
            await EmailManager.ShowComposeNewEmailAsync(email);

            // Clears the debug files.
            DebugUtils.ClearCache();

            // Clears the form and goes back to the menu.
            CancelBugReport();
        }

        private void CancelBugReport()
        {
            // Goes back to the menu.
            DisplayMode = Mode.Menu;

            // Clears the report.
            ClearBugReportForm();
        }

        private void ClearBugReportForm()
        {
            ReportLocationListSelectedIndex = 0;
            ReportProblemListSelectedIndex = 0;
            ReportExtraNotes = null;
        }

        private async void RequestPurchaseSupport()
        {
            try
            {
                // Browse to purchase the support IAP.
                await CurrentApp.RequestProductPurchaseAsync(LicensingManager.CustomSupportProductId);

                // Enables custom support.
                IsAppSupporterContentVisible = true;

                // Shows a progress bar.
                ProgressBarStatusText = "Setting up custom support license...";
                IsProgressBarVisible = true;

                // Validates the license.
                await App.Current.ViewModel.LicensingManager.ValidateCustomSupportLicense();

                // Hides the progress bar.
                IsProgressBarVisible = false;
            }
            catch (Exception ex)
            {
                // The user cancelled.
                DebugUtils.DumpException(ex, "Custom Support Purchase", false);
            }
        }
        #endregion

        private void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
