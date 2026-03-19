using Microsoft.AspNetCore.Components;

namespace Savio.MockServer.Pages;

public partial class About
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string BaseUrl => Navigation.BaseUri.TrimEnd('/');
}
