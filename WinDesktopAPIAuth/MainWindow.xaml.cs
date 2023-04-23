using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Identity.Client;

namespace WinDesktopAPIAuth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Set the API Endpoint
        string apiEndpoint = "https://apimauth.azure-api.net/simpleapiv2/api/getVersion";

        // Set the required scopes for the API call
        // For the Simple API demo, the format should be the "fully qualified" path - i.e. <application id url>/<scope>
        string[] scopes = new string[] { "api://bb2cf962-43e2-4dba-8988-493a9cf414c9/Version.Get" };
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await App.PublicClientApp.GetAccountsAsync();

            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    this.ResultText.Text = "User signed out";
                    this.SignOutButton.Visibility = Visibility.Collapsed;
                }

                catch (MsalException ex)
                {
                    ResultText.Text = $"Error signing out user: {ex.Message}";
                }
            }
        }

        private async void CallAPIButton_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;
            ResultText.Text = string.Empty;
            TokenInfoText.Text = string.Empty;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // The MsalUiRequiredException happens when AcquireTokenInteractive needs to be called
                // i.e. silent token acquisition was not possible
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                }

                catch (MsalException msalex)
                {
                    ResultText.Text = $"Error Acquiring Token: {System.Environment.NewLine}{msalex}";
                }
            }
            catch (Exception ex)
            {
                ResultText.Text = $"Error Acquiring Token Silently: {System.Environment.NewLine}{ex}";
                return;
            }

            if (authResult != null)
            {
                ResultText.Text = await GetHttpContentWithToken(apiEndpoint, authResult.AccessToken);
                DisplayBasicTokenInfo(authResult);
                this.SignOutButton.Visibility = Visibility.Visible;
            }
        }

        private async void CallAPIWithDeviceCodeButton_Click(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;
            ResultText.Text = string.Empty;
            TokenInfoText.Text = string.Empty;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // The MsalUiRequiredException happens when AcquireTokenInteractive needs to be called
                // i.e. silent token acquisition was not possible
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                this.CopyButton.Visibility = Visibility.Visible;
                this.DeviceCode.Visibility = Visibility.Visible;
                this.DeviceCodeUrl.Visibility = Visibility.Visible;

                try
                {
                    authResult = await app.AcquireTokenWithDeviceCode(scopes,
                        deviceCodeResult =>
                        {
                            DeviceCodeUrl.Dispatcher.Invoke(
                                new Action(() => DeviceCodeUrl.Content = $"Please go to {deviceCodeResult.VerificationUrl} and enter the following code:"));
                            DeviceCode.Dispatcher.Invoke(
                                new Action(() => DeviceCode.Content = $"{deviceCodeResult.UserCode}"));
                            CopyButton.Dispatcher.Invoke(
                                new Action(() => CopyButton.Visibility = Visibility.Visible));
                            return Task.FromResult(0);
                        }).ExecuteAsync();
                }

                catch (MsalException msalex)
                {
                    ResultText.Text = $"Error Acquiring Token: {System.Environment.NewLine}{msalex}";
                }
            }
            catch (Exception ex)
            {
                ResultText.Text = $"Error Acquiring Token Silently: {System.Environment.NewLine}{ex}";
                return;
            }

            if (authResult != null)
            {
                ResultText.Text = await GetHttpContentWithToken(apiEndpoint, authResult.AccessToken);
                DisplayBasicTokenInfo(authResult);
                this.SignOutButton.Visibility = Visibility.Visible;
                this.DeviceCode.Visibility = Visibility.Collapsed;
                this.DeviceCodeUrl.Visibility = Visibility.Collapsed;
                this.CopyButton.Visibility = Visibility.Collapsed;
            }
        }

        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;

            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public void DisplayBasicTokenInfo(AuthenticationResult authResult)
        {
            TokenInfoText.Text = "";
            if (authResult != null)
            {
                TokenInfoText.Text += $"Username: {authResult.Account.Username}" + Environment.NewLine;
                TokenInfoText.Text += $"Token expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine;
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(DeviceCode.Content.ToString());
        }
    }
}
