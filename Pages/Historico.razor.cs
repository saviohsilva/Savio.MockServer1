using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Models;
using Savio.MockServer.Security;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class Historico
{
    [CascadingParameter]
    public IModalService Modal { get; set; } = default!;

    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IMockGroupRepository MockGroupRepo { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private string currentUserId = string.Empty;
    private readonly HistoryFilterState filterState = new();

    [SupplyParameterFromQuery(Name = "mockId")]
    public int? MockId { get; set; }

    [SupplyParameterFromQuery(Name = "userId")]
    public string? ScopeUserId { get; set; }

    private List<RequestHistoryListItem>? history;
    private List<MockGroupEntity> groups = [];
    private int currentPage = 1;
    private int pageSize = 100;
    private int totalCount = 0;
    private int TotalPages => (int)Math.Ceiling((double)totalCount / pageSize);
    private string? mockDescription;
    private string? mockRoute;
    private string? mockMethod;
    private bool isCurrentUserAdmin;
    private string? selectedUserId;
    private List<UserScopeOption> userOptions = [];

    // Cache da chave do filtro ativo para evitar COUNT desnecessiário na navegação de página
    private string _cachedFilterKey = string.Empty;
    // Token de debounce para campos de texto
    private CancellationTokenSource? _debounceTokenSource;

    private string sortColumn = "requestedAt";
    private bool sortAscending = false;

    private void ToggleSort(string column)
    {
        if (sortColumn == column)
            sortAscending = !sortAscending;
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
        currentPage = 1;
        _cachedFilterKey = string.Empty;
        _ = LoadHistory();
    }

    private string GetSortIcon(string column)
    {
        if (sortColumn != column) return "bi-chevron-expand";
        return sortAscending ? "bi-chevron-up" : "bi-chevron-down";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Garante que os handles de redimensionamento sejam inicializados após cada render,
        // incluindo o render assíncrono em que a tabela aparece pela primeira vez (após LoadHistory).
        try
        {
            await Js.InvokeVoidAsync("initializeResizableTables");
        }
        catch
        {
            // comportamento opcional de UI
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var state = await AuthState;
            var user = await UserManager.GetUserAsync(state.User);
            currentUserId = user?.Id ?? string.Empty;

            if (user != null)
                await LoadUserOptionsForAdminAsync(user);
        }

        selectedUserId = currentUserId;
        if (isCurrentUserAdmin && !string.IsNullOrWhiteSpace(ScopeUserId) && userOptions.Any(u => u.Id == ScopeUserId))
            selectedUserId = ScopeUserId;

        filterState.UserId = selectedUserId;
        groups = await MockGroupRepo.GetAllAsync(selectedUserId);

        if (MockId.HasValue && !await LoadMockFilterContextAsync())
            return;

        await LoadHistory();
    }

    private async Task LoadUserOptionsForAdminAsync(ApplicationUser user)
    {
        isCurrentUserAdmin = await UserManager.IsInRoleAsync(user, AppRoles.Admin);
        if (!isCurrentUserAdmin) return;

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

    private async Task<bool> LoadMockFilterContextAsync()
    {
        filterState.MockEndpointId = MockId!.Value;
        var mock = await _context.MockEndpoints.AsNoTracking().FirstOrDefaultAsync(m => m.Id == MockId.Value);
        if (mock != null)
        {
            if (!isCurrentUserAdmin && mock.UserId != currentUserId)
            {
                filterState.MockEndpointId = null;
                MockId = null;
                await LoadHistory();
                return false;
            }

            mockRoute = mock.Route;
            mockMethod = mock.Method;
            mockDescription = !string.IsNullOrWhiteSpace(mock.Description)
                ? mock.Description
                : $"{mock.Method} {mock.Route}";
        }
        else
        {
            mockDescription = $"Mock #{MockId.Value}";
        }
        return true;
    }

    private void EditarMock(int mockId)
    {
        Navigation.NavigateTo($"/mock/edit/{mockId}?returnUrl=%2Fhistorico");
    }

    private async Task OnDateRangeChanged((DateTime? fromUtc, DateTime? toUtc, bool isValid) range)
    {
        filterState.SetDateRange(range.fromUtc, range.toUtc, range.isValid);
        currentPage = 1;
        await LoadHistory();
    }

    private async Task LoadHistory()
    {
        filterState.UserId = isCurrentUserAdmin ? selectedUserId : currentUserId;
        var filter = filterState.ToFilter();
        filter.SortColumn = sortColumn;
        filter.SortAscending = sortAscending;
        var filterKey = BuildFilterKey(filter);

        // Só executa COUNT quando o filtro mudou (não na navegação de página)
        if (filterKey != _cachedFilterKey)
        {
            _cachedFilterKey = filterKey;
            totalCount = await HistoryRepo.GetFilteredCountAsync(filter);

            var maxPage = Math.Max(1, TotalPages);
            if (currentPage > maxPage)
                currentPage = maxPage;
        }

        var skip = (currentPage - 1) * pageSize;
        history = await HistoryRepo.SearchListAsync(filter, skip, pageSize);
    }

    private static string BuildFilterKey(RequestHistoryFilter f) =>
        $"{f.MockEndpointId}|{f.MockGroupId}|{f.Method}|{f.RouteContains}|{f.ResponseStatusCode}|{f.TextContains}|{f.FromUtc:O}|{f.ToUtc:O}|{f.UserId}|{f.SortColumn}|{f.SortAscending}";

    // Handler com debounce de 400ms para campos de texto livres
    private async Task OnTextFilterChanged()
    {
        if (_debounceTokenSource != null)
            await _debounceTokenSource.CancelAsync();
        _debounceTokenSource?.Dispose();
        _debounceTokenSource = new CancellationTokenSource();
        var token = _debounceTokenSource.Token;

        try
        {
            await Task.Delay(400, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        currentPage = 1;
        await LoadHistory();
    }

    private async Task ClearHistory()
    {
        var confirmed = await ConfirmActionAsync(
            "Confirmar Limpeza",
            "Confirma a limpeza do histórico? Isso removerá todos os registros.",
            "bi-trash",
            "danger");
        if (!confirmed)
        {
            return;
        }

        var targetUserId = isCurrentUserAdmin ? selectedUserId : currentUserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;

        await HistoryRepo.ClearAsync(targetUserId);

        currentPage = 1;
        _cachedFilterKey = string.Empty; // força re-contagem após limpeza
        await LoadHistory();
    }

    private async Task DeleteItem(int id)
    {
        var confirmed = await ConfirmActionAsync(
            "Confirmar Exclusão",
            "Confirma a exclusão deste item do histórico?",
            "bi-trash",
            "danger");
        if (!confirmed)
        {
            return;
        }

        await HistoryRepo.DeleteByIdAsync(id);
        _cachedFilterKey = string.Empty; // força re-contagem após exclusão
        await LoadHistory();
    }

    private async Task ChangePage(int page)
    {
        if (page < 1 || page > TotalPages) return;

        currentPage = page;
        await LoadHistory();
    }

    private async Task OnPageSizeChanged()
    {
        currentPage = 1;
        await LoadHistory();
    }

    private async Task OnFilterChanged()
    {
        await OnPageSizeChanged();
    }

    private async Task ClearFilters()
    {
        filterState.Clear();
        filterState.UserId = isCurrentUserAdmin ? selectedUserId : currentUserId;
        MockId = null;
        mockDescription = null;
        currentPage = 1;
        await LoadHistory();
    }

    private async Task OnScopeUserChanged()
    {
        if (!isCurrentUserAdmin)
            return;

        if (string.IsNullOrWhiteSpace(selectedUserId) || !userOptions.Any(u => u.Id == selectedUserId))
            selectedUserId = currentUserId;

        filterState.UserId = selectedUserId;
        groups = await MockGroupRepo.GetAllAsync(selectedUserId);
        currentPage = 1;
        await LoadHistory();
    }

    private void ClearMockFilter()
    {
        Navigation.NavigateTo("/historico", forceLoad: true);
    }

    private async Task GoBack()
    {
        await Js.InvokeVoidAsync("history.back");
    }

    private async Task<bool> ConfirmActionAsync(string title, string message, string icon, string iconColor)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), message },
            { nameof(ConfirmDialog.Icon), icon },
            { nameof(ConfirmDialog.IconColor), iconColor }
        };

        var modal = Modal.Show<ConfirmDialog>(title, parameters, new ModalOptions { Size = ModalSize.Small });
        var result = await modal.Result;
        return !result.Cancelled;
    }

    private sealed class UserScopeOption
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
