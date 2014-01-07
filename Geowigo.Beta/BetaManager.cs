using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Geowigo.Beta
{
    public class BetaManager
    {
        private static BetaManager _instance;
        public static BetaManager Instance
        {
            get  
            {
                if (_instance == null)
	            {
                    _instance = new BetaManager();
	            }

                return _instance;
            }
        }

        public IApplicationBar BetaAppBar { get; private set; }

        private UpdateManager _updateManager;

        private BetaManager()
        {
            MakeAppBar();

            _updateManager = new UpdateManager();
            _updateManager.UpdateFound += new EventHandler(UpdateManager_UpdateFound);
            _updateManager.UpdateError += new EventHandler(UpdateManager_UpdateError);
        }
        private void MakeAppBar()
        {
            BetaAppBar = new ApplicationBar();
            
            // Check for updates
            ApplicationBarMenuItem cfu = new ApplicationBarMenuItem("check for updates");
            cfu.Click += new EventHandler(OnClick_CheckForUpdates);
            BetaAppBar.MenuItems.Add(cfu);

            // Report a problem
            ApplicationBarMenuItem rp = new ApplicationBarMenuItem("report a problem");
            rp.Click += new EventHandler(OnClick_ReportProblem);
            BetaAppBar.MenuItems.Add(rp);

            // Get help
            ApplicationBarMenuItem gh = new ApplicationBarMenuItem("talk / get support");
            gh.Click += new EventHandler(OnClick_GetHelp);
            BetaAppBar.MenuItems.Add(gh);
        }

        void OnClick_ReportProblem(object sender, EventArgs e)
        {
            StartReportProblem();
        }

        private void StartReportProblem()
        {
            throw new NotImplementedException();
        }

        void OnClick_GetHelp(object sender, EventArgs e)
        {
            GoToForumThread();
        }

        private void GoToForumThread()
        {
            // Navigates to the forum thread.
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("http://forums.groundspeak.com/GC/index.php?showtopic=315741", UriKind.Absolute);
            task.Show();
        }

        void OnClick_CheckForUpdates(object sender, EventArgs e)
        {
            _updateManager.BeginCheckForUpdate();
        }

        void UpdateManager_UpdateFound(object sender, EventArgs e)
        {
            if (_updateManager.HasNewerVersion)
            {
                _updateManager.ShowMessageBox();
            }
            else
            {
                MessageBox.Show(String.Format("You have the lastest version ({0}).", _updateManager.LatestVersion.VersionRaw));
            }
        }

        void UpdateManager_UpdateError(object sender, EventArgs e)
        {
            if (MessageBox.Show("Unable to check for updates. Please check the official forum thread to get support or info about latest updates.", "Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                GoToForumThread();
            }
        }
    }
}
