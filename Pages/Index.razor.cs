using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Security;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class Index : IDisposable
{
    [CascadingParameter]
    public IModalService Modal { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    private List<MockEndpoint> mocks = [];
    private string? currentUserId;
    private string? currentAlias;
    private bool isCurrentUserAdmin;
    private string? selectedUserId;
    private List<UserScopeOption> userOptions = [];

    private DateTime? LastAccessUtc =>
        mocks.Any(m => m.LastCalledAt.HasValue)
            ? mocks.Where(m => m.LastCalledAt.HasValue)
                   .Max(m => m.LastCalledAt!.Value)
            : null;

    protected override void OnInitialized()
    {
        TimezoneService.OnOffsetSet += OnTimezoneReady;
    }

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var authState = await AuthState;
            var user = await UserManager.GetUserAsync(authState.User);
            currentUserId = user?.Id;
            currentAlias = user?.Alias;

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
                                : $"{u.UserName} (/{u.Alias})",
                            Alias = u.Alias
                        })
                        .ToListAsync();
                }
            }
        }

        selectedUserId = currentUserId;
        await LoadMocksAsync();
    }

    private void OnTimezoneReady() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        TimezoneService.OnOffsetSet -= OnTimezoneReady;
        GC.SuppressFinalize(this);
    }

    private async Task LoadMocksAsync()
    {
        var scopeUserId = isCurrentUserAdmin ? selectedUserId : currentUserId;
        mocks = await MockService.GetAllMocksAsync(scopeUserId);
    }

    private async Task OnScopeUserChanged()
    {
        if (!isCurrentUserAdmin)
            return;

        if (string.IsNullOrWhiteSpace(selectedUserId) || !userOptions.Any(u => u.Id == selectedUserId))
            selectedUserId = currentUserId;

        await LoadMocksAsync();
    }

    private void NavigateToCreate()
    {
        Navigation.NavigateTo("/mock/create");
    }

    private void NavigateToEdit(string id)
    {
        Navigation.NavigateTo($"/mock/edit/{id}");
    }

    private async Task DeleteMock(string id)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), "Tem certeza que deseja excluir este mock?" },
            { nameof(ConfirmDialog.Icon), "bi-trash" },
            { nameof(ConfirmDialog.IconColor), "danger" }
        };
        var options = new ModalOptions { Size = ModalSize.Small };
        var modal = Modal.Show<ConfirmDialog>("Confirmar Exclusão", parameters, options);
        var result = await modal.Result;

        if (!result.Cancelled)
        {
            await MockService.DeleteMockAsync(id);
            await LoadMocksAsync();
            StateHasChanged();
        }
    }

    private async Task DuplicateMock(string id)
    {
        await MockService.DuplicateMockAsync(id);
        await LoadMocksAsync();
        StateHasChanged();
    }

    private void ViewHistory(string mockId)
    {
        if (int.TryParse(mockId, out int numericId))
        {
            Navigation.NavigateTo($"/historico?mockId={numericId}");
        }
    }

    private async Task TestMock(MockEndpoint mock)
    {
        var scopedAlias = currentAlias;
        if (isCurrentUserAdmin && !string.IsNullOrWhiteSpace(selectedUserId))
            scopedAlias = userOptions.FirstOrDefault(u => u.Id == selectedUserId)?.Alias;

        var aliasPrefix = !string.IsNullOrEmpty(scopedAlias) ? $"/{scopedAlias}" : string.Empty;
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        var url = $"{baseUri}{aliasPrefix}{mock.Route}";
        await JS.InvokeVoidAsync("window.open", url, "_blank");
    }

    private sealed class UserScopeOption
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
    }
}
