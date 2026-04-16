using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Data.Repositories;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class Historico
{
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private IMockGroupRepository MockGroupRepo { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private string currentUserId = string.Empty;
    private readonly HistoryFilterState filterState = new();

    [SupplyParameterFromQuery(Name = "mockId")]
    public int? MockId { get; set; }

    private List<RequestHistoryEntity>? history;
    private List<MockGroupEntity> groups = [];
    private int currentPage = 1;
    private int pageSize = 100;
    private int totalCount = 0;
    private int TotalPages => (int)Math.Ceiling((double)totalCount / pageSize);
    private string? mockDescription;
    private string? mockRoute;
    private string? mockMethod;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var state = await AuthState;
            var user = await UserManager.GetUserAsync(state.User);
            currentUserId = user?.Id ?? string.Empty;
        }

        filterState.UserId = currentUserId;
        groups = await MockGroupRepo.GetAllAsync(currentUserId);

        if (MockId.HasValue)
        {
            filterState.MockEndpointId = MockId.Value;
            var mock = await _context.MockEndpoints.FindAsync(MockId.Value);
            if (mock != null)
            {
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
        }

        await LoadHistory();
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
        var filter = filterState.ToFilter();

        totalCount = await HistoryRepo.GetFilteredCountAsync(filter);
        var skip = (currentPage - 1) * pageSize;
        history = await HistoryRepo.SearchAsync(filter, skip, pageSize);

        var maxPage = Math.Max(1, TotalPages);
        if (currentPage > maxPage)
        {
            currentPage = maxPage;
            skip = (currentPage - 1) * pageSize;
            history = await HistoryRepo.SearchAsync(filter, skip, pageSize);
        }
    }

    private async Task ClearHistory()
    {
        var confirmed = await Js.InvokeAsync<bool>("confirm", "Confirma a limpeza do histórico? Isso removerá todos os registros.");
        if (!confirmed)
        {
            return;
        }

        await HistoryRepo.ClearAsync(currentUserId);

        currentPage = 1;
        await LoadHistory();
    }

    private async Task DeleteItem(int id)
    {
        var confirmed = await Js.InvokeAsync<bool>("confirm", "Confirma a exclusão deste item do histórico?");
        if (!confirmed)
        {
            return;
        }

        await HistoryRepo.DeleteByIdAsync(id);
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
        filterState.UserId = currentUserId;
        MockId = null;
        mockDescription = null;
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
}
