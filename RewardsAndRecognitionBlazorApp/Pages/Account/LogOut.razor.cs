using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace RewardsAndRecognitionBlazorApp.Pages.Account
{
    public partial class LogOut : ComponentBase
    {
        private bool isProcessing;
        private bool hasLoggedOut;
        private bool isUserLoggedIn;

        [Inject] private HttpClient Http { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ILogger<LogOut> Logger { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private RewardsAndRecognitionBlazorApp.Services.BrowserAuthStateProvider AuthProvider { get; set; }
        [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] private RewardsAndRecognitionBlazorApp.ViewModels.UserSession Session { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Http.BaseAddress = new Uri("http://127.0.0.1:5104/");
            await Session.InitializeAsync(JS);
            isUserLoggedIn = Session.IsLoggedIn;
        }

        private async Task OnLogoutClicked()
        {
            isProcessing = true;

            try
            {
                // Use the auth provider to clear session and notify components
                if (AuthStateProvider is RewardsAndRecognitionBlazorApp.Services.SessionStorageAuthStateProvider sessionProvider)
                {
                    await sessionProvider.MarkUserAsLoggedOutAsync();
                }
                else if (AuthProvider != null)
                {
                    await AuthProvider.MarkUserAsLoggedOutAsync();
                }

                // Clear token from storage as a fallback
                try { await JS.InvokeVoidAsync("browserStorage.removeItem", "authToken"); } catch { }
                Http.DefaultRequestHeaders.Authorization = null;

                // Call API logout if available
                try { await Http.PostAsync("api/account/logout", content: null); } catch { }

                Logger.LogInformation("User logged out.");
                hasLoggedOut = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during logout.");
                hasLoggedOut = true;
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }

        private void GoHome()
        {
            Navigation.NavigateTo("/", forceLoad: true);
        }
    }
}

