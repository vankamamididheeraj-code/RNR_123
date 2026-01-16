using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RewardsAndRecognitionBlazorApp.ViewModels;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;

namespace RewardsAndRecognitionBlazorApp.Pages.User
{
    public partial class Create
    {
        [SupplyParameterFromQuery(Name = "returnUrl")]
        public string? ReturnUrl { get; set; }

        private string CancelUrl => string.IsNullOrWhiteSpace(ReturnUrl) ? "/users" : ReturnUrl!;

        private CreateUserView Model { get; set; } = new();

        private List<string> Roles { get; set; } = new();
        private List<TeamView> Teams { get; set; } = new();

        private bool IsSubmitting { get; set; }
        private string? ErrorMessage { get; set; }

        [Inject] private IJSRuntime JS { get; set; } = default!;

        private bool _showPassword;
        private string PasswordInputType => _showPassword ? "text" : "password";
        private string EyeIcon => _showPassword ? "fa-eye-slash" : "fa-eye";

        private bool TeamDisabled => !string.Equals(Model.SelectedRole, "Employee", StringComparison.OrdinalIgnoreCase);

        protected override async Task OnInitializedAsync()
        {
            await ApplyBearerTokenIfPresentAsync();

            Roles = await Http.GetFromJsonAsync<List<string>>("api/user/roles") ?? new();

            // TeamController returns Team entity; we map to TeamView for dropdown
            var teamApi = await Http.GetFromJsonAsync<List<TeamApiDto>>("api/team") ?? new();
            Teams = teamApi.Select(t => new TeamView
            {
                Id = t.Id,
                Name = t.Name ?? "",
                TeamLeadId = t.TeamLeadId ?? "",
                ManagerId = t.ManagerId ?? "",
                DirectorId = t.DirectorId ?? "",
                IsDeleted = t.IsDeleted
            }).ToList();

            if (TeamDisabled)
                Model.TeamId = null;
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
            catch { }
        }

        private void TogglePassword() => _showPassword = !_showPassword;

        private void OnRoleChanged(ChangeEventArgs _)
        {
            if (TeamDisabled)
                Model.TeamId = null;
        }

        private async Task CreateUser()
        {
            ErrorMessage = null;
            IsSubmitting = true;

            try
            {
                // must match API: POST api/user/register
                var resp = await Http.PostAsJsonAsync("api/user/register", Model);

                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = await resp.Content.ReadAsStringAsync();
                    return;
                }

                Nav.NavigateTo(CancelUrl);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        // minimal DTO that matches Team entity JSON (doesn't require full Team model)
        private sealed class TeamApiDto
        {
            public Guid Id { get; set; }
            public string? Name { get; set; }
            public string? TeamLeadId { get; set; }
            public string? ManagerId { get; set; }
            public string? DirectorId { get; set; }
            public bool IsDeleted { get; set; }
        }
    }
}
