using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RewardsAndRecognitionBlazorApp.ViewModels;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RewardsAndRecognitionBlazorApp.Pages.User
{
    public partial class Delete
    {
        [Parameter] public string? Id { get; set; }

        // /users/delete/{id}?returnUrl=/users
        [SupplyParameterFromQuery(Name = "returnUrl")]
        public string? ReturnUrl { get; set; }

        private string CancelUrl => string.IsNullOrWhiteSpace(ReturnUrl) ? "/users" : ReturnUrl!;

        private UserView? User { get; set; }
        private bool IsBusy { get; set; }
        private string? ErrorMessage { get; set; }

        [Inject] private IJSRuntime JS { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                ErrorMessage = "User ID is required.";
                return;
            }

            await ApplyBearerTokenIfPresentAsync();
            await LoadUserAsync();
        }

        private async Task ApplyBearerTokenIfPresentAsync()
        {
            try
            {
                var token = await JS.InvokeAsync<string>("browserStorage.getItem", "authToken");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch
            {
                // ignore if JS not available (e.g., prerender)
            }
        }

        private async Task LoadUserAsync()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = null;

                // API (recommended from our earlier fix): GET api/user/{id} returns UserView
                User = await Http.GetFromJsonAsync<UserView>($"api/user/{Uri.EscapeDataString(Id!)}");

                if (User is null)
                    ErrorMessage = "User not found.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ConfirmDeleteAsync()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                ErrorMessage = "User ID is required.";
                return;
            }

            // optional extra confirm (you can remove if you prefer only page-level confirmation)
            var confirmed = await JS.InvokeAsync<bool>("confirm", $"Delete user '{User?.Name}'?");
            if (!confirmed) return;

            try
            {
                IsBusy = true;
                ErrorMessage = null;

                var res = await Http.DeleteAsync($"api/user/{Uri.EscapeDataString(Id)}");
                res.EnsureSuccessStatusCode();

                Nav.NavigateTo(CancelUrl);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
