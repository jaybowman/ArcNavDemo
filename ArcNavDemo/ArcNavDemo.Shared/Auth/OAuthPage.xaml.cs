using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if __IOS__
using UIKit;
#endif
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
#if __ANDROID__
using Android.App;
using Application = Xamarin.Forms.Application;
using System.IO;
#endif

namespace ArcNavDemo.Auth
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OAuthPage : ContentPage
    { 
        private static double _latitude = 29.224589; // 28.907124539990157;
        private static double _longitude = -82.101045; // -81.97479891040035;
                    //publix at pearl britian 29.224589, -82.101045

        private ArcGISPortal _portal;
        private Map _map; //  = new Map(Basemap.CreateStreetsVector());
        private BasemapType basemapType = BasemapType.StreetsVector;
        
        private int levelOfDetail = 13;

        private Uri _routeService;
        private string _trafficLayerURL = "https://utility.arcgis.com/usrsvcs/appservices/6IPRsqbQjXmnoSz5/rest/services/World/Traffic/MapServer";
        private string _serverUrl = "https://www.arcgis.com/sharing/rest";
        private string _serviceArea = "https://utility.arcgis.com/usrsvcs/appservices/ejTBgNK72EWdNKqp/rest/services/World/ServiceAreas/NAServer/ServiceArea_World/solve";
        private string _clientId = "4LAh8YGROZk34kJR";
        private string _redirectURI = "nav-app://auth";
        private const string _clientSecret = "60f9b814a86645a0bb4f5c12685ccb31";

        // A TaskCompletionSource to store the result of a login task.
        private TaskCompletionSource<Credential> _loginTaskCompletionSrc;
        // Page for the user to enter login information.
        private LoginPage _loginPage;

        public OAuthPage()
        {
            _routeService = new Uri(App.Current.Resources["RouteServiceProxy"] as string);
            _trafficLayerURL = "http://traffic.arcgis.com/arcgis/rest/services/World/Traffic/MapServer";
            InitializeComponent();
           
            CreateNewMap();
            SetOAuthInfo();
            //AddTrafficLayer();
        }

        private void CreateNewMap()
        {
            MyMapView.Map = new Map(basemapType, _latitude, _longitude, levelOfDetail);
        }

        private async void AddTrafficLayer()
        {

            try
            {
                _routeService = new Uri("https://route.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World/solve");

                var route = await RouteTask.CreateAsync(_routeService);


                ArcGISMapImageLayer traffic = new ArcGISMapImageLayer(new Uri(_trafficLayerURL));
                MyMapView.Map.OperationalLayers.Add(traffic);
            }
            catch (Exception ex)
            {
              await DisplayAlert("Error", ex.Message, "OK");                
            }
        }

       
        private async void SetOAuthInfo()
        {
            Esri.ArcGISRuntime.Security.ServerInfo serverInfo = new ServerInfo
            {
                ServerUri = new Uri(_serverUrl),
                TokenAuthenticationType = TokenAuthenticationType.ArcGISToken,                
                OAuthClientInfo = new OAuthClientInfo { ClientId = _clientId, RedirectUri = new Uri(_redirectURI), ClientSecret = _clientSecret }
            };

            AuthenticationManager.Current.RegisterServer(serverInfo);

            // Define a challenge handler method for the AuthenticationManager.
            // This method handles getting credentials when a secured resource is encountered.
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);
            // Create the login UI (will display when the user accesses a secured resource).
            _loginPage = new LoginPage();

            // Set up event handlers for when the user completes the login entry or cancels.
            _loginPage.OnLoginInfoEntered += LoginInfoEntered;
            _loginPage.OnCanceled += LoginCanceled;

            // define the credential request
            //CredentialRequestInfo cri = new CredentialRequestInfo
            //{
            //    // token authentication
            //    AuthenticationType = AuthenticationType.Token,
            //    // define the service URI
            //    ServiceUri = new Uri(_serverUrl),
            //    // OAuth (implicit flow) token type
            //    GenerateTokenOptions = new GenerateTokenOptions
            //    {
            //        TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit                    
            //    }
            //};
            //var cred = await AuthenticationManager.Current.GetCredentialAsync(cri, false).ConfigureAwait(false);                              

            // connecting to the portal will use an available credential (based on the server URL)

            // _portal = await Esri.ArcGISRuntime.Portal.ArcGISPortal.CreateAsync(new Uri(_serviceArea));

        }

        // AuthenticationManager.ChallengeHandler function that prompts the user for login information to create a credential.
        private async Task<Credential> CreateCredentialAsync(CredentialRequestInfo info)
        {
            // Return if authentication is already in process.
            if (_loginTaskCompletionSrc != null && !_loginTaskCompletionSrc.Task.IsCanceled) { return null; }

            // Create a new TaskCompletionSource for the login operation.
            // Passing the CredentialRequestInfo object to the constructor will make it available from its AsyncState property.
            _loginTaskCompletionSrc = new TaskCompletionSource<Credential>(info);

            // Provide a title for the login form (show which service needs credentials).
#if WINDOWS_UWP
            // UWP doesn't have ServiceUri.GetLeftPart (could use ServiceUri.AbsoluteUri for all, but why not use a compilation condition?)
            _loginPage.TitleText = "Login for " + info.ServiceUri.AbsoluteUri;
#else
            _loginPage.TitleText = "Login for " + info.ServiceUri.GetLeftPart(UriPartial.Path);
#endif
            // Show the login controls on the UI thread.
            // OnLoginInfoEntered event will return the values entered (username and password).
            Device.BeginInvokeOnMainThread(async () => await Navigation.PushAsync(_loginPage));

            // Return the login task, the result will be ready when completed (user provides login info and clicks the "Login" button)
            return await _loginTaskCompletionSrc.Task;
        }

        // Handle the OnLoginEntered event from the login UI.
        // LoginEventArgs contains the username and password that were entered.
        private async void LoginInfoEntered(object sender, LoginEventArgs e)
        {
            // Make sure the task completion source has all the information needed.
            if (_loginTaskCompletionSrc == null ||
                _loginTaskCompletionSrc.Task == null ||
                _loginTaskCompletionSrc.Task.AsyncState == null)
            {
                return;
            }

            try
            {
                // Get the associated CredentialRequestInfo (will need the URI of the service being accessed).
                CredentialRequestInfo requestInfo = (CredentialRequestInfo)_loginTaskCompletionSrc.Task.AsyncState;

                // Create a token credential using the provided username and password.
                TokenCredential userCredentials = await AuthenticationManager.Current.GenerateCredentialAsync
                                            (requestInfo.ServiceUri,
                                             e.Username,
                                             e.Password,
                                             requestInfo.GenerateTokenOptions);

                // Set the task completion source result with the ArcGIS network credential.
                // AuthenticationManager is waiting for this result and will add it to its Credentials collection.
                _loginTaskCompletionSrc.TrySetResult(userCredentials);
            }
            catch (Exception ex)
            {
                // Unable to create credential, set the exception on the task completion source.
                _loginTaskCompletionSrc.TrySetException(ex);
            }
            finally
            {
                // Dismiss the login controls.
                await Navigation.PopAsync();
            }
        }

        private void LoginCanceled(object sender, EventArgs e)
        {
            // Dismiss the login controls.
            Navigation.PopAsync();

            // Cancel the task completion source task.
            _loginTaskCompletionSrc.TrySetCanceled();
        }

        protected void ToolbarItem_Clicked(object source, EventArgs e)
        {
            AddTrafficLayer();
        }
    }
}