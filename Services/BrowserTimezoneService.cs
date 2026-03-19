namespace Savio.MockServer.Services;

public class BrowserTimezoneService
{
    private int? _offsetMinutes;

    public bool IsInitialized => _offsetMinutes.HasValue;

    public void SetOffset(int offsetMinutes)
    {
        _offsetMinutes = offsetMinutes;
    }

    /// <summary>
    /// Converte um DateTime UTC para o horário local do navegador.
    /// </summary>
    public DateTime? ToLocalTime(DateTime? utcTime)
    {
        if (utcTime == null || !_offsetMinutes.HasValue)
            return utcTime;

        var dt = utcTime.Value.Kind == DateTimeKind.Local
            ? utcTime.Value.ToUniversalTime()
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        return dt.AddMinutes(-_offsetMinutes.Value);
    }

    /// <summary>
    /// Formata um DateTime? UTC para string no fuso do navegador. Retorna "-" se nulo.
    /// </summary>
    public string FormatLocalTime(DateTime? utcTime, string format = "dd/MM HH:mm:ss")
        => ToLocalTime(utcTime)?.ToString(format) ?? "-";

    /// <summary>
    /// Formata um DateTime UTC para string no fuso do navegador.
    /// </summary>
    public string FormatLocalTime(DateTime utcTime, string format = "dd/MM/yyyy HH:mm")
        => ToLocalTime(utcTime)?.ToString(format) ?? utcTime.ToString(format);
}
