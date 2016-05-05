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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Phone.Storage.SharedAccess;

namespace Geowigo
{
    public partial class App : Application
    {
        #region Nested Classes
        internal class AppUriMapper : UriMapperBase
        {
            public override Uri MapUri(Uri uri)
            {
                string tempUri = uri.ToString();

                // File association launch
                if (tempUri.StartsWith("/FileTypeAssociation"))
                {
                    try
                    {
                        // Get the file ID (after "fileToken=").
                        int fileIDIndex = tempUri.IndexOf("fileToken=") + 10;
                        string fileID = tempUri.Substring(fileIDIndex);

                        // Get the file name.
                        string incomingFileName = SharedStorageAccessManager.GetSharedFileName(fileID);

                        // Get the file extension.
                        string incomingFileType = System.IO.Path.GetExtension(incomingFileName);

                        // Map the recognized files to different pages.
                        switch (incomingFileType)
                        {
                            case ".gwc":
                                return new Uri(String.Format("/Views/CartridgeInfoPage.xaml?{0}={1}", ViewModels.CartridgeInfoViewModel.FileTokenKey, fileID), UriKind.Relative);
                            default:
                                return new Uri("/MainPage.xaml", UriKind.Relative);
                        }
                    }
                    catch (Exception)
                    {
                        return uri;
                    }
                }

                // Otherwise perform normal launch.
                return uri;
            }
        }
        #endregion
        
        #region Fields

		private ViewModels.AppViewModel _appViewModel;

		private Models.WherigoModel _model;

		#endregion

		#region Properties
		/// <summary>
		/// Permet d'accéder facilement au frame racine de l'application téléphonique.
		/// </summary>
		/// <returns>Frame racine de l'application téléphonique.</returns>
		public PhoneApplicationFrame RootFrame { get; private set; }

		/// <summary>
		/// Gets the current Application object.
		/// </summary>
		public static new App Current
		{
			get
			{
				return (App)Application.Current;
			}
		}

		/// <summary>
		/// Gets the application-wide view model that controls app flow.
		/// </summary>
		public ViewModels.AppViewModel ViewModel
		{
			get
			{
				if (_appViewModel == null)
				{
					_appViewModel = new ViewModels.AppViewModel() { Model = this.Model };
				}

				return _appViewModel;
			}
		}

		/// <summary>
		/// Gets the application-wide model.
		/// </summary>
		public Models.WherigoModel Model
		{
			get
			{
				if (_model == null)
				{
					_model = new Models.WherigoModel();
				}

				return _model;
			}
		}
		#endregion

        /// <summary>
        /// Constructeur pour l'objet Application.
        /// </summary>
        public App()
        {
            // Gestionnaire global pour les exceptions non interceptées. 
            UnhandledException += Application_UnhandledException;

            // Starting BugSense.
            BugSense.BugSenseHandler.Instance.InitAndStartSession(new BugSense.Core.Model.ExceptionManager(this), RootFrame, "88834254");

            // Initialisation Silverlight standard
            InitializeComponent();

            // Initialisation spécifique au téléphone
            InitializePhoneApplication();

            // Affichez des informations de profilage graphique lors du débogage.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Affichez les compteurs de fréquence des trames actuels.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Affichez les zones de l'application qui sont redessinées dans chaque frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Activez le mode de visualisation d'analyse hors production, 
                // qui montre les zones d'une page sur lesquelles une accélération GPU est produite avec une superposition colorée.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Désactivez la détection d'inactivité de l'application en définissant la propriété UserIdleDetectionMode de l'objet
                // PhoneApplicationService de l'application sur Désactivé.
                // Attention :- À utiliser uniquement en mode de débogage. Les applications qui désactivent la détection d'inactivité de l'utilisateur continueront de s'exécuter
                // et seront alimentées par la batterie lorsque l'utilisateur ne se sert pas du téléphone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code à exécuter lorsque l'application démarre (par exemple, à partir de Démarrer)
        // Ce code ne s'exécute pas lorsque l'application est réactivée
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            
        }

        // Code à exécuter lorsque l'application est activée (affichée au premier plan)
        // Ce code ne s'exécute pas lorsque l'application est démarrée pour la première fois
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
			ViewModel.HandleAppActivated(!e.IsApplicationInstancePreserved);
        }

        // Code à exécuter lorsque l'application est désactivée (envoyée à l'arrière-plan)
        // Ce code ne s'exécute pas lors de la fermeture de l'application
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
			ViewModel.HandleAppDeactivated();
        }

        // Code à exécuter lors de la fermeture de l'application (par exemple, lorsque l'utilisateur clique sur Précédent)
        // Ce code ne s'exécute pas lorsque l'application est désactivée
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code à exécuter en cas d'échec d'une navigation
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Échec d'une navigation ; arrêt dans le débogueur
                System.Diagnostics.Debugger.Break();
            }

            // Dump the sith out!
            Utils.DebugUtils.DumpException(e.Exception);
        }

        // Code à exécuter sur les exceptions non gérées
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {				
				// Une exception non gérée s'est produite ; arrêt dans le débogueur
                System.Diagnostics.Debugger.Break();
            }

			// Dump the sith out!
			Utils.DebugUtils.DumpException(e.ExceptionObject);
        }

        #region Initialisation de l'application téléphonique

        // Éviter l'initialisation double
        private bool phoneApplicationInitialized = false;

        // Ne pas ajouter de code supplémentaire à cette méthode
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Créez le frame, mais ne le définissez pas encore comme RootVisual ; cela permet à l'écran de
            // démarrage de rester actif jusqu'à ce que l'application soit prête pour le rendu.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Assign the URI-mapper class to the application frame.
            RootFrame.UriMapper = new AppUriMapper();

            // Gérer les erreurs de navigation
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Garantir de ne pas retenter l'initialisation
            phoneApplicationInitialized = true;
        }

        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render.
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Removes the useless handler.
            RootFrame.Navigated -= CompleteInitializePhoneApplication;

			// Injects the media element template into the root frame.
			RootFrame.Template = App.Current.Resources["AudioPlayerContentTemplate"] as ControlTemplate;
        }

        #endregion
    }
}