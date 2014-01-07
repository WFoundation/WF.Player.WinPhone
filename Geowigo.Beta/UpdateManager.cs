using System;
using System.Reflection;
using System.Windows;
using System.Text;
using Microsoft.Phone.Tasks;
using System.Net;

namespace Geowigo.Beta
{
    public class UpdateManager
    {
        #region Nested Classes

        public class UpdateInfo
        {
            public string VersionRaw { get; set; }

            public Version Version
            {
                get
                {
                    return Version.Parse(VersionRaw);
                }
            }

            public string Changelog { get; set; }

            public string Incentive { get; set; }

            public DateTime ReleaseDate 
            {
                get
                {
                    System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dtDateTime = dtDateTime.AddSeconds(long.Parse(ReleaseDateRaw)).ToLocalTime();
                    return dtDateTime;
                }
            }

            public string ReleaseDateRaw { get; set; }

            public string UrlRaw { get; set; }

            public Uri Url
            {
                get
                {
                    return new Uri(UrlRaw, UriKind.Absolute);
                }
            }
        }

        #endregion
        
        #region Properties
        public event EventHandler UpdateFound;

        public event EventHandler UpdateError;

        public UpdateInfo LatestVersion { get; private set; }

        public bool HasNewerVersion
        {
            get
            {
                return this.LatestVersion != null &&
                    GetAssemblyVersion(Assembly.GetCallingAssembly()).CompareTo(this.LatestVersion.Version) < 0;
            }
        } 
        #endregion

        #region Members

        private Uri _manifestUri = new Uri("http://s272323109.onlinehome.fr/temp/geowigo/geowigo_update.json", UriKind.Absolute); 

        #endregion

        private static Version GetAssemblyVersion(Assembly asm)
        {
            var parts = asm.FullName.Split(',');
            return new Version(parts[1].Split('=')[1]);
        }

        public void BeginCheckForUpdate()
        {
            // Downloads the json string.
            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadStringCompleted);
            webClient.DownloadStringAsync(_manifestUri);
        }

        private void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                // Gets the json response.
                string json = e.Result;

                // Gets the latest version object from the json.
                UpdateInfo ui = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateInfo>(json);
                LatestVersion = ui;

                // Raises the event.
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (UpdateFound != null)
                    {
                        UpdateFound(this, EventArgs.Empty);
                    }
                }));
            }
            catch (Exception)
            {
                if (UpdateError != null)
                {
                    UpdateError(this, EventArgs.Empty);
                }
            }
        }

        public void ShowMessageBox()
        {
            // Builds the message string.
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("An update has been found.");
            sb.AppendLine("Because you are running a BETA version, you need to perform this update manually.");
            sb.AppendLine();
            sb.AppendLine("Version " + this.LatestVersion.VersionRaw + " - " + this.LatestVersion.ReleaseDate.ToShortDateString());
            sb.AppendLine(this.LatestVersion.Changelog);
            sb.AppendLine();
            sb.AppendLine(this.LatestVersion.Incentive);
            sb.AppendLine();
            sb.AppendLine("Click on OK to open a page with more instructions.");

            // Shows the message box.
            if (MessageBox.Show(sb.ToString(), "Update Geowigo Beta", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                // Goes to internet explorer.
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = this.LatestVersion.Url;
                task.Show();
            }
        }
    }
}
