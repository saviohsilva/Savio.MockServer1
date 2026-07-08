using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Security;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class UnmockedRequests
{
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private List<UnmockedRequestEntity>? unmockedRequests;
    private int currentPage = 1;
    private readonly int pageSize = 50;
    private int totalCount = 0;
    private int TotalPages => (int)Math.Ceiling((double)totalCount / pageSize);

    private string currentUserId = string.Empty;
    private bool isCurrentUserAdmin;
    private string? selectedUserId;
    private List<UserScopeOption> userOptions = [];

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var state = await AuthState;
            var user = await UserManager.GetUserAsync(state.User);
            currentUserId = user?.Id ?? string.Empty;

            if (user != null)
            {
                isCurrentUserAdmin = await UserManager.IsInRoleAsync(user, AppRoles.Admin);
                if (isCurrentUserAdmin)
                {
                    userOptions = await UserManager.Users
                        .OrderBy(u => u.UserName)
                        .Select(u => new UserScopeOption
                        {
                            Id = u.Id,
                            Label = string.IsNullOrWhiteSpace(u.Alias)
                                ? (u.UserName ?? "(sem usuário)")
                                : $"{u.UserName} (/{u.Alias})"
                        })
                        .ToListAsync();
                }
            }
        }

        selectedUserId = currentUserId;
        await LoadUnmockedRequests();
    }

    private async Task OnScopeUserChanged()
    {
        if (!isCurrentUserAdmin) return;

        if (string.IsNullOrWhiteSpace(selectedUserId) || !userOptions.Any(u => u.Id == selectedUserId))
            selectedUserId = currentUserId;

        currentPage = 1;
        await LoadUnmockedRequests();
    }

    private async Task LoadUnmockedRequests()
    {
        var userId = isCurrentUserAdmin ? selectedUserId : currentUserId;
        totalCount = await UnmockedRepo.GetTotalCountAsync(userId);
        var skip = (currentPage - 1) * pageSize;
        unmockedRequests = await UnmockedRepo.GetAllAsync(userId, skip, pageSize);
    }

    private async Task ChangePage(int page)
    {
        if (page < 1 || page > TotalPages) return;

        currentPage = page;
        await LoadUnmockedRequests();
    }

    private void CreateMockFromRequest(UnmockedRequestEntity request)
    {
        // Quando o admin cria um mock a partir de uma rota de outro usuário, repassa
        // o userId do dono da rota para que o mock seja criado para esse usuário.
        if (isCurrentUserAdmin
            && !string.IsNullOrWhiteSpace(request.UserId)
            && request.UserId != currentUserId)
        {
            Navigation.NavigateTo(
                $"/mock/create?from=unmocked&id={request.Id}&targetUserId={Uri.EscapeDataString(request.UserId)}");
        }
        else
        {
            Navigation.NavigateTo($"/mock/create?from=unmocked&id={request.Id}");
        }
    }

    private async Task DeleteRequest(int id)
    {
        await UnmockedRepo.DeleteAsync(id);
        await LoadUnmockedRequests();
    }

    private sealed class UserScopeOption
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
