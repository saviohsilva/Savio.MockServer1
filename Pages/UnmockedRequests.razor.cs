using Microsoft.AspNetCore.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class UnmockedRequests
{
    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    private List<UnmockedRequestEntity>? unmockedRequests;
    private int currentPage = 1;
    private int pageSize = 50;
    private int totalCount = 0;
    private int totalPages => (int)Math.Ceiling((double)totalCount / pageSize);

    protected override async Task OnInitializedAsync()
    {
        await LoadUnmockedRequests();
    }

    private async Task LoadUnmockedRequests()
    {
        totalCount = await UnmockedRepo.GetTotalCountAsync();
        var skip = (currentPage - 1) * pageSize;
        unmockedRequests = await UnmockedRepo.GetAllAsync(skip, pageSize);
    }

    private async Task ChangePage(int page)
    {
        if (page < 1 || page > totalPages) return;

        currentPage = page;
        await LoadUnmockedRequests();
    }

    private void CreateMockFromRequest(UnmockedRequestEntity request)
    {
        Navigation.NavigateTo($"/mock/create?from=unmocked&id={request.Id}");
    }

    private async Task DeleteRequest(int id)
    {
        await UnmockedRepo.DeleteAsync(id);
        await LoadUnmockedRequests();
    }
}
