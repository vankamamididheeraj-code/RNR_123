using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RewardsAndRecognitionBlazorApp.ViewModels;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RewardsAndRecognitionBlazorApp.Pages.User
{
    public partial class Index
    {
        private List<UserView> Users { get; set; } = new();
        private bool IsBusy { get; set; }
        private string? Error { get; set; }

        private bool ShowDeleted { get; set; } = false;

        // Paging state
        private int PageNumber { get; set; } = 1;
        private int PageSize { get; set; } = 5;
        private long TotalCount { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await ApplyBearerTokenIfPresentAsync();
            await LoadAsync();
        }

        public async Task OnShowDeletedChanged()
        {
            await LoadAsync();
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
                // ignore (e.g., server prerender or JS not available)
            }
        }

        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                Error = null;

                var url = $"api/user/paged?pageNumber={PageNumber}&pageSize={PageSize}&showDeleted={ShowDeleted.ToString().ToLowerInvariant()}";

                var res = await Http.GetAsync(url);

                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized || res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Error = "You do not have permission to view users.";
                    Users = new List<UserView>();
                    TotalCount = 0;
                    return;
                }

                res.EnsureSuccessStatusCode();

                var paged = await res.Content.ReadFromJsonAsync<RewardsAndRecognitionBlazorApp.ViewModels.PagedResult<RewardsAndRecognitionBlazorApp.ViewModels.UserView>>();
                if (paged != null)
                {
                    PageNumber = paged.PageNumber;
                    PageSize = paged.PageSize;
                    TotalCount = paged.TotalCount;
                    Users = paged.Items.ToList();
                }
                else
                {
                    Users = new List<UserView>();
                    TotalCount = 0;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadPage(int newPage)
        {
            PageNumber = newPage;
            await LoadAsync();
        }


        private async Task DeleteAsync(string userId)
        {
            var confirmed = await JS.InvokeAsync<bool>("usersUi.confirmDelete");
            if (!confirmed) return;

            try
            {
                IsBusy = true;
                Error = null;

                var res = await Http.DeleteAsync($"api/user/{Uri.EscapeDataString(userId)}");
                res.EnsureSuccessStatusCode();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RestoreAsync(string userId)
        {
            try
            {
                IsBusy = true;
                Error = null;

                var res = await Http.PostAsync($"api/user/{Uri.EscapeDataString(userId)}/restore", content: null);
                res.EnsureSuccessStatusCode();

                await LoadAsync();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
