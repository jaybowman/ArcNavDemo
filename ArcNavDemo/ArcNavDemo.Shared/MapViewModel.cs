using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using Xamarin.Forms;


namespace ArcNavDemo.Shared
{
    /// <summary>
    /// Provides map data to an application
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
        public static double latitude = 28.907124539990157;
        public static double longitude = -81.97479891040035;
        public double _theVillagesScale = 124762.7156655228955;

        // Create and set initial map area
        public Envelope _theVillagesEnvelope = new Envelope(-82.121556, 28.690528, -81.887883, 29.001611, SpatialReferences.Wgs84);
        // Create central point where map is centered
        public MapPoint _theVillagesCentralPoint = new MapPoint(longitude, latitude, SpatialReferences.Wgs84);
        public MapPoint Destination { get; set; }

        private Map _map; //  = new Map(Basemap.CreateStreetsVector());
        private BasemapType basemapType = BasemapType.StreetsVector;
        //authentication

        private int levelOfDetail = 11;
        private string trafficLayerURL = "https://traffic.arcgis.com/arcgis/rest/services/World/Traffic/MapServer";
      
       
        public Map Map
        {
            get { return _map; }
            set { _map = value; OnPropertyChanged(); }
        }

        public MapViewModel()
        {
            //Load();
            CreateNewMap();
            //AddTrafficLayer();
        }

        private void CreateNewMap()
        {
            Map = new Map(basemapType, latitude, longitude, levelOfDetail);
        }
        
        private void AddTrafficLayer()
        {
            // SetOAuthInfo();
            ArcGISMapImageLayer traffic = new ArcGISMapImageLayer(new Uri(trafficLayerURL));
            Map.OperationalLayers.Add(traffic);
        }





        private async void Load()
        {

            //"https://arc5.thevillages.com/arcgis/rest/services/TSGLOCATE2/GeocodeServer"
            //string geo = Application.Current.Resources["GeoCodeServer"] as string;
            //_geocoder = new LocatorTask(new Uri(geo));
            //await _geocoder.LoadAsync();
            string ms = "https://arc7.thevillages.com/arcgis/rest/services/PUBLICMAP26W/MapServer";
            //string ms = Application.Current.Resources["MapServer"] as string;
            Uri mapService = new Uri(ms);

            var layer = new ArcGISTiledLayer(mapService);
         
            Map = new Esri.ArcGISRuntime.Mapping.Map(new Basemap(layer));
                                 
            double scale = 24000d;
            if (Destination == null)
            {
                scale = 200000d;
                Destination = _theVillagesCentralPoint;
            }
            Viewpoint startingPoint = new Viewpoint(Destination, scale);

            Map.InitialViewpoint = startingPoint;            

            await Map.LoadAsync();
        }

        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class OAuthAuthorize : IOAuthAuthorizeHandler
    {
        //private Window _window;
        private TaskCompletionSource<IDictionary<string, string>> _tcs;
        private string _callbackUrl;
        private string _authorizeUrl;

      

        private static IDictionary<string, string> DecodeParameters(Uri uri)
        {
            var answer = string.Empty;
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                answer = uri.Fragment.Substring(1);
            }
            else if (!string.IsNullOrEmpty(uri.Query))
            {
                answer = uri.Query.Substring(1);
            }
            var keyValueDictionary = new Dictionary<string, string>();
            var keysAndValues = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kvString in keysAndValues)
            {
                var pair = kvString.Split('=');
                string key = pair[0];
                string value = string.Empty;
                if (key.Length > 1)
                {
                    value = Uri.UnescapeDataString(pair[1]);
                }
                keyValueDictionary.Add(key, value);
            }
            return keyValueDictionary;
        }

        public Task<IDictionary<string, string>> AuthorizeAsync(Uri serviceUri, Uri authorizeUri, Uri callbackUri)
        {
            throw new NotImplementedException();
        }
    }
}
