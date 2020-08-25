using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using Xamarin.Forms;
using ArcNavDemo.Auth;

namespace ArcNavDemo
{
    public class App : Xamarin.Forms.Application
    {
        public App()
        {
            // Deployed applications must be licensed at the Lite level or greater. 
            // See https://developers.arcgis.com/licensing for further details.

            // Initialize the ArcGIS Runtime before any components are created.
            ArcGISRuntimeEnvironment.SetLicense("runtimebasic,1000,rud000252796,none,MJJ47AZ7G349NERL1216");
            ArcGISRuntimeEnvironment.Initialize();

            Resources.Add("RouteServiceProxy", "https://utility.arcgis.com/usrsvcs/appservices/uoBbS3YqJ0gRi2Ab/rest/services/World/Route/NAServer/Route_World/solve");

            // The root page of your application
            MainPage = new NavigationPage(new MapPage()); //   (new OAuthPage());
        }
    }
}
