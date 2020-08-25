using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Navigation;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using Xamarin.Auth;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using ArcNavDemo.Auth;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Portal;
using System.Linq;
using Esri.ArcGISRuntime.Mapping;

#if __IOS__
using Xamarin.Forms.Platform.iOS;
    using UIKit;
#endif

#if __ANDROID__
using Android.App;
using Application = Xamarin.Forms.Application;
using System.IO;
using Esri.ArcGISRuntime.Security;
#endif

namespace ArcNavDemo
{
    public partial class MapPage : ContentPage
    {
        // app id
        private string _clientId = "4LAh8YGROZk34kJR";
        private string _redirectURI = "nav-app://auth";
        private const string _clientSecret = "for my eyes only";
        private string _serverUrl = "https://www.arcgis.com/sharing/rest";
        private Credential _token;

        // Variables for tracking the navigation route.
        private RouteTracker _tracker;
        private RouteResult _routeResult;
        private Route _route;
        private RouteTask _routeTask;
        private RouteParameters _routeParams;

        // List of driving directions for the route.
        private IReadOnlyList<DirectionManeuver> _directionsList;

        // Cancellation token for speech synthesizer.
        private CancellationTokenSource _speechToken = new CancellationTokenSource();

        // Graphics to show progress along the route.
        private Graphic _routeAheadGraphic;
        private Graphic _routeTraveledGraphic;

        private readonly MapPoint _conventionCenter = new MapPoint(-81.9728128552126, 28.8994314411087, SpatialReferences.Wgs84);
        private readonly MapPoint _aerospaceMuseum = new MapPoint(-81.9980352826826, 28.9167414059663, SpatialReferences.Wgs84);        // GPX file that contains simulated location data for driving route.
      

        private readonly Uri _carRoutingUri = new Uri("https://arc7.thevillages.com/arcgis/rest/services/CARROUTES/NAServer/Route");
        private readonly Uri _mapService = new Uri("https://arc7.thevillages.com/arcgis/rest/services/PUBLICMAP26/MapServer");

        // A TaskCompletionSource to store the result of a login task.
        private TaskCompletionSource<Credential> _loginTaskCompletionSrc;

        // Page for the user to enter login information.
        private LoginPage _loginPage;

        public MapPage()
        {
            string routeService = App.Current.Resources["RouteServiceProxy"] as string;
            _carRoutingUri = new Uri(routeService);

            InitializeComponent();
            SetAuthInfo();
            GetRoute();
        }

        private void SetAuthInfo()
        {
            // I don't think i need this for token authentication.
            //Esri.ArcGISRuntime.Security.ServerInfo serverInfo = new ServerInfo
            //{
            //    ServerUri = new Uri(_serverUrl),
            //    TokenAuthenticationType = TokenAuthenticationType.ArcGISToken,
            //    OAuthClientInfo = new OAuthClientInfo { ClientId = _clientId, RedirectUri = new Uri(_redirectURI), ClientSecret = _clientSecret }
            //};
            //AuthenticationManager.Current.RegisterServer(serverInfo);

            // Define a challenge handler method for the AuthenticationManager.
            // This method handles getting credentials when a secured resource is encountered.
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);

            // Create the login UI (will display when the user accesses a secured resource).
            _loginPage = new LoginPage();

            // Set up event handlers for when the user completes the login entry or cancels.
            _loginPage.OnLoginInfoEntered += LoginInfoEntered;
            _loginPage.OnCanceled += LoginCanceled;
        }

      
        private async void GetRoute()
        {           

            try
            {              
                // what do i need to do to get credentials token ? Automatically done when a request to a secure service by Chanllengehandler.            

                _routeTask = await RouteTask.CreateAsync(_carRoutingUri);

                // Get the default route parameters.
                _routeParams = await _routeTask.CreateDefaultParametersAsync();

                // Explicitly set values for parameters.
                _routeParams.ReturnDirections = true;
                _routeParams.ReturnStops = true;
                _routeParams.ReturnRoutes = true;
                _routeParams.OutputSpatialReference = SpatialReferences.Wgs84;

                // Create stops for each location.
                Stop stop1 = new Stop(_conventionCenter) { Name = "Canal St." };
                Stop stop2 = new Stop(_aerospaceMuseum) { Name = "Oxford Ln, The Villages" };

                // Assign the stops to the route parameters.
                List<Stop> stopPoints = new List<Stop> { stop1, stop2 };
                _routeParams.SetStops(stopPoints);

                // Get the route results.
                _routeResult = await _routeTask.SolveRouteAsync(_routeParams);
                _route = _routeResult.Routes[0];

                // Add a graphics overlay for the route graphics.
                MyMapView.GraphicsOverlays.Add(new GraphicsOverlay());

                // Add graphics for the stops.
                SimpleMarkerSymbol stopSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.OrangeRed, 20);
                MyMapView.GraphicsOverlays[0].Graphics.Add(new Graphic(_conventionCenter, stopSymbol));
                MyMapView.GraphicsOverlays[0].Graphics.Add(new Graphic(_aerospaceMuseum, stopSymbol));

                // Create a graphic (with a dashed line symbol) to represent the route.
                _routeAheadGraphic = new Graphic(_route.RouteGeometry) { Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, Color.BlueViolet, 5) };

                // Create a graphic (solid) to represent the route that's been traveled (initially empty).
                _routeTraveledGraphic = new Graphic { Symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.LightBlue, 3) };

                // Add the route graphics to the map view.
                MyMapView.GraphicsOverlays[0].Graphics.Add(_routeAheadGraphic);
                MyMapView.GraphicsOverlays[0].Graphics.Add(_routeTraveledGraphic);

                // Set the map viewpoint to show the entire route.
                await MyMapView.SetViewpointGeometryAsync(_route.RouteGeometry, 100);

               

            }
            catch (Exception e)
            {
                await DisplayAlert("Error", e.Message, "OK");
            }
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


        // Map initialization logic is contained in MapViewModel.cs

        private async void ToolbarItem_Clicked(object sender, System.EventArgs e)
        {
            var btn = sender as ToolbarItem;
            // DisplayAlert("Button", $"Button pressed: {btn.Text}", "Cancel");
            switch (btn.Text)
            {
                case "START":
                    StartNavigation();
                    break;
                case "AUTO":
                    RecenterButton();
                    break;
                case "ROUTE":
                    GetRoute();
                    break;
            }
        }

        protected void AddressSearch_TextChanged(object sender, System.EventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void StartNavigation()
        {        

            // Get the directions for the route.
            _directionsList = _route.DirectionManeuvers;

            // Create a route tracker.
            _tracker = new RouteTracker(_routeResult, 0);
            _tracker.NewVoiceGuidance += SpeakDirection;

            // Handle route tracking status changes.
            _tracker.TrackingStatusChanged += TrackingStatusUpdated;

            // Check if this route task supports rerouting.
            if (_routeTask.RouteTaskInfo.SupportsRerouting)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    MessagesTextBlock.Text = "Reroute is enabled";
                });
                // Enable automatic re-routing.
                await _tracker.EnableReroutingAsync(_routeTask, _routeParams, ReroutingStrategy.ToNextWaypoint, false);

                // Handle re-routing completion to display updated route graphic and report new status.
                _tracker.RerouteStarted += RerouteStarted;
                _tracker.RerouteCompleted += RerouteCompleted;
            }

            // Turn on navigation mode for the map view.
            MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Navigation;
            MyMapView.LocationDisplay.AutoPanModeChanged += AutoPanModeChanged;

            // Add simulated data source for the location display.
            var simulationParameters = new SimulationParameters(DateTimeOffset.Now, 40.0);
            var simulatedDataSource = new SimulatedLocationDataSource();
            simulatedDataSource.SetLocationsWithPolyline(_route.RouteGeometry, simulationParameters);
            //MyMapView.LocationDisplay.DataSource = new RouteTrackerDisplayLocationDataSource(simulatedDataSource, _tracker);

            // Use this instead if you want real location:
            MyMapView.LocationDisplay.DataSource = new RouteTrackerDisplayLocationDataSource(new SystemLocationDataSource(), _tracker);

            // Enable the location display (this will start the location data source).
            MyMapView.LocationDisplay.IsEnabled = true;
        }

        private void RerouteStarted(object sender, EventArgs e)
        {
            // Remove the event listeners for tracking status changes while the route tracker recalculates.
            _tracker.NewVoiceGuidance -= SpeakDirection;
            _tracker.TrackingStatusChanged -= TrackingStatusUpdated;

            Device.BeginInvokeOnMainThread(() =>
            {
                MessagesTextBlock.Text = "Reroute started";
            });
        }

        private void RerouteCompleted(object sender, RouteTrackerRerouteCompletedEventArgs e)
        {
            // Get the new directions.
            _route = e.TrackingStatus.RouteResult.Routes[0];
            _directionsList = _route.DirectionManeuvers;

            // Re-add the event listeners for tracking status changes.
            _tracker.NewVoiceGuidance += SpeakDirection;
            _tracker.TrackingStatusChanged += TrackingStatusUpdated;

            Device.BeginInvokeOnMainThread(() =>
            {
                //await DisplayAlert("Reroute", "Reroute completed", "OK")
                MessagesTextBlock.Text = "Reroute completed event fired!!";
;
            });
        }

        private void TrackingStatusUpdated(object sender, RouteTrackerTrackingStatusChangedEventArgs e)
        {
            TrackingStatus status = e.TrackingStatus;

            // Start building a status message for the UI.
            System.Text.StringBuilder statusMessageBuilder = new System.Text.StringBuilder("Route Status:\n");

            // Check if navigation is on route.
            if (status.IsOnRoute && !status.IsRouteCalculating)
            {
                // Check the destination status.
                if (status.DestinationStatus == DestinationStatus.NotReached || status.DestinationStatus == DestinationStatus.Approaching)
                {
                    statusMessageBuilder.AppendLine("Distance remaining: " +
                                                status.RouteProgress.RemainingDistance.DisplayText + " " +
                                                status.RouteProgress.RemainingDistance.DisplayTextUnits.PluralDisplayName);

                    statusMessageBuilder.AppendLine("Time remaining: " +
                                                    status.RouteProgress.RemainingTime.ToString(@"hh\:mm\:ss"));

                    if (status.CurrentManeuverIndex + 1 < _directionsList.Count)
                    {
                        statusMessageBuilder.AppendLine("Next direction: " + _directionsList[status.CurrentManeuverIndex + 1].DirectionText);
                    }

                    // Set geometries for progress and the remaining route.
                    _routeAheadGraphic.Geometry = status.RouteProgress.RemainingGeometry;
                    _routeTraveledGraphic.Geometry = status.RouteProgress.TraversedGeometry;
                }
                else if (status.DestinationStatus == DestinationStatus.Reached)
                {
                    statusMessageBuilder.AppendLine("Destination reached.");

                    // Set the route geometries to reflect the completed route.
                    _routeAheadGraphic.Geometry = null;
                    _routeTraveledGraphic.Geometry = status.RouteResult.Routes[0].RouteGeometry;
                    MyMapView.LocationDisplay.IsEnabled = false;
                }
            }
            else
            {
                statusMessageBuilder.AppendLine("Off route!");
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                // Show the status information in the UI.
                MessagesTextBlock.Text = statusMessageBuilder.ToString().TrimEnd('\n').TrimEnd('\r');
            });
        }

        private async void SpeakDirection(object sender, RouteTrackerNewVoiceGuidanceEventArgs e)
        {
            // Say the direction using voice synthesis.
            if (e.VoiceGuidance.Text?.Length > 0)
            {
                _speechToken.Cancel();
                _speechToken = new CancellationTokenSource();
                await Xamarin.Essentials.TextToSpeech.SpeakAsync(e.VoiceGuidance.Text, _speechToken.Token);
            }
        }

        private void AutoPanModeChanged(object sender, LocationDisplayAutoPanMode e)
        {
            // Turn the recenter button on or off when the location display changes to or from navigation mode.
            //RecenterButton.IsEnabled = e != LocationDisplayAutoPanMode.Navigation;
        }

        private void RecenterButton()
        {
            // Change the mapview to use navigation mode.
            MyMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Navigation;
        }

        public void Dispose()
        {
            // Stop currently playing speech.
            _speechToken.Cancel();

            // Stop the tracker.
            if (_tracker != null)
            {
                _tracker.TrackingStatusChanged -= TrackingStatusUpdated;
                _tracker.NewVoiceGuidance -= SpeakDirection;
                _tracker.RerouteStarted -= RerouteStarted;
                _tracker.RerouteCompleted -= RerouteCompleted;
                _tracker = null;
            }

            // Stop the location data source.
            MyMapView.LocationDisplay?.DataSource?.StopAsync();
        }
 
        #region Esri Authentication...
             
      

        #endregion
        
    }

    // This location data source uses an input data source and a route tracker.
    // The location source that it updates is based on the snapped-to-route location from the route tracker.
    public class RouteTrackerDisplayLocationDataSource : LocationDataSource
    {
        private LocationDataSource _inputDataSource;
        private RouteTracker _routeTracker;

        public RouteTrackerDisplayLocationDataSource(LocationDataSource dataSource, RouteTracker routeTracker)
        {
            // Set the data source
            _inputDataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

            // Set the route tracker.
            _routeTracker = routeTracker ?? throw new ArgumentNullException(nameof(routeTracker));

            // Change the tracker location when the source location changes.
            _inputDataSource.LocationChanged += InputLocationChanged;

            // Update the location output when the tracker location updates.
            _routeTracker.TrackingStatusChanged += TrackingStatusChanged;
        }

        private void InputLocationChanged(object sender, Esri.ArcGISRuntime.Location.Location e)
        {
            // Update the tracker location with the new location from the source (simulation or GPS).
            _routeTracker.TrackLocationAsync(e);
        }

        private void TrackingStatusChanged(object sender, RouteTrackerTrackingStatusChangedEventArgs e)
        {
            // Check if the tracking status has a location.
            if (e.TrackingStatus.DisplayLocation != null)
            {
                // Call the base method for LocationDataSource to update the location with the tracked (snapped to route) location.
                UpdateLocation(e.TrackingStatus.DisplayLocation);
            }
        }
        protected override Task OnStartAsync() => _inputDataSource.StartAsync();

        protected override Task OnStopAsync() => _inputDataSource.StopAsync();
    }
}
