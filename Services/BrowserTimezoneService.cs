namespace Savio.MockServer.Services;

public class BrowserTimezoneService
{
    private int? _offsetMinutes;

    public bool IsInitialized => _offsetMinutes.HasValue;

    /// <summary>
    /// Evento disparado quando o offset do navegador é definido.
    /// Páginas podem subscrever para se re-renderizar com o horário correto.
    /// </summary>
    public event Action? OnOffsetSet;

    public void SetOffset(int offsetMinutes)
    {
        _offsetMinutes = offsetMinutes;
        OnOffsetSet?.Invoke();
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
    /// Converte um DateTime UTC para o horário local do navegador (versão não-nulável).
    /// </summary>
    public DateTime ToLocalTime(DateTime utcTime)
        => ToLocalTime((DateTime?)utcTime) ?? utcTime;

    /// <summary>
    /// Converte um DateTime no horário do navegador de volta para UTC.
    /// </summary>
    public DateTime? ToLocalTimeReverse(DateTime? localTime)
    {
        if (localTime == null || !_offsetMinutes.HasValue)
            return localTime;

        return localTime.Value.AddMinutes(_offsetMinutes.Value);
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
        => ToLocalTime(utcTime).ToString(format);
}
