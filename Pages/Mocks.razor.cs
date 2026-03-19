using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Savio.MockServer.Components;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class Mocks
{
    [CascadingParameter]
    public IModalService Modal { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private readonly MockFilter filter = new();
    private string filterActiveString = string.Empty;
    private string filterGroupString = string.Empty;
    private string activeTab = "mocks";

    private List<MockEndpoint> allFilteredMocks = new();
    private List<MockGroup> groups = new();
    private HashSet<string> selectedMocks = new();
    private int? expandedGroupId;

    private string sortColumn = "route";
    private bool sortAscending = true;

    private string? alertMessage;
    private string alertClass = "alert-info";
    private string alertIcon = "bi-info-circle";
    private List<string> alertDetails = new();
    private string? currentUserId;

    private List<MockEndpoint> sortedMocks => ApplySorting(allFilteredMocks);

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var authState = await AuthState;
            var user = await UserManager.GetUserAsync(authState.User);
            currentUserId = user?.Id;
        }

        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("tab", out var tabValue))
        {
            activeTab = tabValue.ToString();
        }

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        filter.IsActive = string.IsNullOrEmpty(filterActiveString) ? null : bool.Parse(filterActiveString);
        filter.MockGroupId = string.IsNullOrEmpty(filterGroupString) ? null : int.Parse(filterGroupString);
        filter.UserId = currentUserId;
        allFilteredMocks = await MockService.GetFilteredMocksAsync(filter);
        groups = await MockService.GetAllGroupsAsync(currentUserId);
        selectedMocks.Clear();
    }

    private void OnFilterChanged()
    {
        _ = LoadDataAsync();
    }

    private void ClearFilters()
    {
        filter.Clear();
        filterActiveString = string.Empty;
        filterGroupString = string.Empty;
        _ = LoadDataAsync();
    }

    // ── Ordenação ──

    private void ToggleSort(string column)
    {
        if (sortColumn == column)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
    }

    private string GetSortIcon(string column)
    {
        if (sortColumn != column)
        {
            return "bi-chevron-expand";
        }

        return sortAscending ? "bi-chevron-up" : "bi-chevron-down";
    }

    private List<MockEndpoint> ApplySorting(List<MockEndpoint> mocks)
    {
        var ordered = sortColumn switch
        {
            "status" => sortAscending
                ? mocks.OrderBy(m => m.IsActive)
                : mocks.OrderByDescending(m => m.IsActive),
            "method" => sortAscending
                ? mocks.OrderBy(m => m.Method)
                : mocks.OrderByDescending(m => m.Method),
            "route" => sortAscending
                ? mocks.OrderBy(m => m.Route)
                : mocks.OrderByDescending(m => m.Route),
            "statusCode" => sortAscending
                ? mocks.OrderBy(m => m.StatusCode)
                : mocks.OrderByDescending(m => m.StatusCode),
            "description" => sortAscending
                ? mocks.OrderBy(m => m.Description)
                : mocks.OrderByDescending(m => m.Description),
            "group" => sortAscending
                ? mocks.OrderBy(m => m.MockGroupName ?? string.Empty)
                : mocks.OrderByDescending(m => m.MockGroupName ?? string.Empty),
            "calls" => sortAscending
                ? mocks.OrderBy(m => m.CallCount)
                : mocks.OrderByDescending(m => m.CallCount),
            _ => mocks.OrderBy(m => m.Route)
        };

        return ordered.ToList();
    }

    // ── Navegação ──

    private void ToggleGroup(int groupId)
    {
        expandedGroupId = expandedGroupId == groupId ? null : groupId;
    }

    private void NavigateToCreateMock() => Navigation.NavigateTo("/mock/create");
    private void NavigateToCreateMockInGroup(int groupId) => Navigation.NavigateTo($"/mock/create?groupId={groupId}");
    private void NavigateToEdit(string id) => Navigation.NavigateTo($"/mock/edit/{id}");
    private void NavigateToCreateGroup() => Navigation.NavigateTo("/group/create");
    private void NavigateToEditGroup(int id) => Navigation.NavigateTo($"/group/edit/{id}");

    private void ViewHistory(string mockId)
    {
        if (int.TryParse(mockId, out int numericId))
        {
            Navigation.NavigateTo($"/historico?mockId={numericId}");
        }
    }

    // ── Seleção em massa ──

    private void ToggleSelectAllMocks(ChangeEventArgs e)
    {
        if ((bool)(e.Value ?? false))
        {
            selectedMocks = sortedMocks.Select(m => m.Id).ToHashSet();
        }
        else
        {
            selectedMocks.Clear();
        }
    }

    private void ToggleMockSelection(string id, bool selected)
    {
        if (selected)
        {
            selectedMocks.Add(id);
        }
        else
        {
            selectedMocks.Remove(id);
        }
    }

    private async Task ActivateSelectedMocksAsync()
    {
        var errors = new List<string>();
        foreach (var id in selectedMocks.ToList())
        {
            var (success, error) = await MockService.SetMockActiveAsync(id, true);
            if (!success && error != null)
            {
                errors.Add(error);
            }
        }

        if (errors.Count > 0)
        {
            ShowAlert("alert-warning", "bi-exclamation-triangle", "Alguns mocks não puderam ser ativados:", errors);
        }
        else
        {
            ShowAlert("alert-success", "bi-check-circle", "Mocks selecionados ativados com sucesso.");
        }

        await LoadDataAsync();
    }

    private async Task DeactivateSelectedMocksAsync()
    {
        foreach (var id in selectedMocks.ToList())
        {
            await MockService.SetMockActiveAsync(id, false);
        }

        ShowAlert("alert-success", "bi-check-circle", "Mocks selecionados desativados com sucesso.");
        await LoadDataAsync();
    }

    private async Task DeleteSelectedMocks()
    {
        var count = selectedMocks.Count;
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), $"Tem certeza que deseja excluir {count} mock(s) selecionado(s)?" },
            { nameof(ConfirmDialog.Icon), "bi-trash" },
            { nameof(ConfirmDialog.IconColor), "danger" }
        };
        var options = new ModalOptions { Size = ModalSize.Small };
        var modal = Modal.Show<ConfirmDialog>("Confirmar Exclusão", parameters, options);
        var result = await modal.Result;

        if (!result.Cancelled)
        {
            foreach (var id in selectedMocks.ToList())
            {
                await MockService.DeleteMockAsync(id);
            }

            ShowAlert("alert-success", "bi-check-circle", $"{count} mock(s) excluído(s) com sucesso.");
            await LoadDataAsync();
        }
    }

    // ── Ações de mock ──

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
            await LoadDataAsync();
        }
    }

    private async Task DuplicateMock(string id)
    {
        var (success, error) = await MockService.DuplicateMockAsync(id);
        if (success)
        {
            ShowAlert("alert-success", "bi-check-circle", "Mock duplicado com sucesso. A cópia foi criada como inativa.");
        }
        else
        {
            ShowAlert("alert-danger", "bi-exclamation-triangle", error ?? "Erro ao duplicar mock.");
        }

        await LoadDataAsync();
    }

    private async Task RemoveFromGroup(string mockId)
    {
        await MockService.RemoveMockFromGroupAsync(mockId);
        await LoadDataAsync();
    }

    // ── Ações de grupo ──

    private async Task ActivateGroup(int groupId)
    {
        var (success, error, conflicts) = await MockService.ActivateGroupMocksAsync(groupId);
        if (!success)
        {
            ShowAlert("alert-warning", "bi-exclamation-triangle", error!, conflicts);
        }
        else
        {
            ShowAlert("alert-success", "bi-check-circle", "Todos os mocks do grupo foram ativados.");
        }

        await LoadDataAsync();
    }

    private async Task DeactivateGroup(int groupId)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), "Deseja desativar todos os mocks deste grupo?" },
            { nameof(ConfirmDialog.Icon), "bi-toggle-off" },
            { nameof(ConfirmDialog.IconColor), "secondary" }
        };
        var options = new ModalOptions { Size = ModalSize.Small };
        var modal = Modal.Show<ConfirmDialog>("Confirmar Desativação", parameters, options);
        var result = await modal.Result;

        if (!result.Cancelled)
        {
            await MockService.DeactivateGroupMocksAsync(groupId);
            ShowAlert("alert-success", "bi-check-circle", "Todos os mocks do grupo foram desativados.");
            await LoadDataAsync();
        }
    }

    private async Task DuplicateGroup(int groupId)
    {
        var (success, error) = await MockService.DuplicateGroupAsync(groupId);
        if (success)
        {
            ShowAlert("alert-success", "bi-check-circle", "Agrupamento duplicado com sucesso. Todos os mocks da cópia foram criados como inativos.");
        }
        else
        {
            ShowAlert("alert-danger", "bi-exclamation-triangle", error ?? "Erro ao duplicar agrupamento.");
        }

        await LoadDataAsync();
    }

    private async Task DeleteGroup(int groupId, string groupName)
    {
        var parameters = new ModalParameters
        {
            { nameof(ConfirmDialog.Message), $"Deseja excluir o grupo \"{groupName}\"? Os mocks do grupo não serão excluídos, apenas ficarão sem agrupamento." },
            { nameof(ConfirmDialog.Icon), "bi-trash" },
            { nameof(ConfirmDialog.IconColor), "danger" }
        };
        var options = new ModalOptions { Size = ModalSize.Small };
        var modal = Modal.Show<ConfirmDialog>("Confirmar Exclusão de Grupo", parameters, options);
        var result = await modal.Result;

        if (!result.Cancelled)
        {
            await MockService.DeleteGroupAsync(groupId);
            await LoadDataAsync();
        }
    }

    // ── Filtro auxiliar ──

    private List<MockEndpoint> ApplyFilterToList(List<MockEndpoint> mocks)
    {
        var result = mocks.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Method))
        {
            result = result.Where(m => m.Method == filter.Method);
        }

        if (!string.IsNullOrWhiteSpace(filter.RouteContains))
        {
            result = result.Where(m => m.Route.Contains(filter.RouteContains, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.IsActive.HasValue)
        {
            result = result.Where(m => m.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.DescriptionContains))
        {
            result = result.Where(m =>
                m.Description.Contains(filter.DescriptionContains, StringComparison.OrdinalIgnoreCase) ||
                m.Route.Contains(filter.DescriptionContains, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.MockGroupId.HasValue)
        {
            if (filter.MockGroupId.Value == -1)
            {
                result = result.Where(m => m.MockGroupId == null);
            }
            else
            {
                result = result.Where(m => m.MockGroupId == filter.MockGroupId.Value);
            }
        }

        return result.ToList();
    }

    // ── Alertas ──

    private void ShowAlert(string cssClass, string icon, string message, List<string>? details = null)
    {
        alertClass = cssClass;
        alertIcon = icon;
        alertMessage = message;
        alertDetails = details ?? new List<string>();
    }

    private void ClearAlert()
    {
        alertMessage = null;
        alertDetails.Clear();
    }
}
