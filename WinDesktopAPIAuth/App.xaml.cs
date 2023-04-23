using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Identity.Client;

namespace WinDesktopAPIAuth
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            _clientApp = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
                .WithDefaultRedirectUri()
                .Build();
        }

        private static string ClientId = "1ad95b3a-d1cc-4bb2-88a1-28bad04085a9";

        private static string Tenant = "3f439c65-6d32-4778-8e53-683629c93558";

        private static IPublicClientApplication _clientApp;

        public static IPublicClientApplication PublicClientApp { get { return _clientApp; } }
    }
}
