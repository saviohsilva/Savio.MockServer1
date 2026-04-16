using Microsoft.AspNetCore.Components;
using System.Globalization;
using Savio.MockServer.Services;

namespace Savio.MockServer.Components;

public partial class DateTimeLocalFilter
{
    private const string DateTimeLocalFormat = "yyyy-MM-ddTHH:mm";

    private string? fromText;
    private string? toText;
    private string? validationMessage;

    [Parameter] public DateTime? FromUtc { get; set; }
    [Parameter] public DateTime? ToUtc { get; set; }

    [Parameter] public EventCallback<(DateTime? fromUtc, DateTime? toUtc, bool isValid)> OnRangeChanged { get; set; }

    [Inject] private BrowserTimezoneService TimezoneService { get; set; } = default!;

    protected override void OnParametersSet()
    {
        fromText = FromUtc.HasValue ? ToLocalDisplay(FromUtc.Value) : null;
        toText = ToUtc.HasValue ? ToLocalDisplay(ToUtc.Value) : null;
        validationMessage = null;
    }

    private async Task OnFromInput(Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        fromText = e.Value?.ToString();
        await Emit();
    }

    private async Task OnToInput(Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        toText = e.Value?.ToString();
        await Emit();
    }

    private async Task Emit()
    {
        var fromLocal = ParseDateTimeLocal(fromText);
        var toLocal = ParseDateTimeLocal(toText);

        validationMessage = null;
        var isValid = true;

        if (fromLocal.HasValue && toLocal.HasValue && toLocal.Value < fromLocal.Value)
        {
            validationMessage = "A data final não pode ser inferior à data inicial.";
            isValid = false;
        }

        var fromUtc = isValid ? ToUtcFromLocal(fromLocal) : null;
        var toUtc = isValid ? ToUtcFromLocal(toLocal) : null;

        await OnRangeChanged.InvokeAsync((fromUtc, toUtc, isValid));
    }

    private string ToLocalDisplay(DateTime utc)
    {
        var local = TimezoneService.ToLocalTime(utc);
        return local.ToString(DateTimeLocalFormat, CultureInfo.InvariantCulture);
    }

    private DateTime? ToUtcFromLocal(DateTime? local)
    {
        if (local == null) return null;
        return TimezoneService.ToLocalTimeReverse(local.Value);
    }

    private static DateTime? ParseDateTimeLocal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParseExact(value, DateTimeLocalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Local);

        return null;
    }
}
