using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Savio.MockServer.Data.Entities;
using Savio.MockServer.Models;
using Savio.MockServer.Services;

namespace Savio.MockServer.Pages;

public partial class MockGroupEditor
{
    [Parameter]
    public int Id { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    private string groupName = string.Empty;
    private string groupDescription = string.Empty;
    private string? saveError;

    private List<MockEndpoint> groupMocks = new();
    private List<MockEndpoint> availableMocks = new();

    private bool IsEdit => Id > 0;
    private string? currentUserId;

    protected override async Task OnInitializedAsync()
    {
        if (AuthState != null)
        {
            var authState = await AuthState;
            var user = await UserManager.GetUserAsync(authState.User);
            currentUserId = user?.Id;
        }

        if (IsEdit)
        {
            var group = await MockService.GetGroupByIdAsync(Id);
            if (group != null)
            {
                groupName = group.Name;
                groupDescription = group.Description;
                groupMocks = group.MockEndpoints;
            }

            availableMocks = await MockService.GetStandaloneMocksAsync();
        }
    }

    private async Task SaveGroup()
    {
        saveError = null;

        if (string.IsNullOrWhiteSpace(groupName))
        {
            saveError = "O nome do grupo é obrigatório.";
            return;
        }

        if (IsEdit)
        {
            var (success, error) = await MockService.UpdateGroupAsync(Id, groupName, groupDescription, currentUserId);
            if (!success)
            {
                saveError = error;
                return;
            }
        }
        else
        {
            var (success, error) = await MockService.AddGroupAsync(groupName, groupDescription, currentUserId);
            if (!success)
            {
                saveError = error;
                return;
            }
        }

        Navigation.NavigateTo("/mocks?tab=groups");
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/mocks?tab=groups");
    }

    private void EditMock(string id)
    {
        Navigation.NavigateTo($"/mock/edit/{id}");
    }

    private async Task AddToGroup(string mockId)
    {
        await MockService.AddMockToGroupAsync(mockId, Id);
        await RefreshMockListsAsync();
    }

    private async Task RemoveFromGroup(string mockId)
    {
        await MockService.RemoveMockFromGroupAsync(mockId);
        await RefreshMockListsAsync();
    }

    private async Task RefreshMockListsAsync()
    {
        var group = await MockService.GetGroupByIdAsync(Id);
        if (group != null)
        {
            groupMocks = group.MockEndpoints;
        }

        availableMocks = await MockService.GetStandaloneMocksAsync();
    }
}
