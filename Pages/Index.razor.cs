using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
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
        }
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
        mocks = await MockService.GetAllMocksAsync(currentUserId);
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
        var aliasPrefix = !string.IsNullOrEmpty(currentAlias) ? $"/{currentAlias}" : string.Empty;
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        var url = $"{baseUri}{aliasPrefix}{mock.Route}";
        await JS.InvokeVoidAsync("window.open", url, "_blank");
    }
}
