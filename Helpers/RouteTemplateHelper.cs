namespace Savio.MockServer.Helpers;

public static class RouteTemplateHelper
{
    /// <summary>
    /// Verifica se <paramref name="actual"/> corresponde ao template de rota <paramref name="template"/>.
    /// Segmentos no formato {paramName} ou {paramName:constraint} aceitam qualquer valor.
    /// </summary>
    public static bool MatchesTemplate(string template, string actual)
    {
        var templateSegments = template.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var actualSegments = actual.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (templateSegments.Length != actualSegments.Length)
            return false;

        for (var i = 0; i < templateSegments.Length; i++)
        {
            var ts = templateSegments[i];

            if (ts.StartsWith('{') && ts.EndsWith('}'))
                continue;

            if (!ts.Equals(actualSegments[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Retorna true se a rota contém ao menos um segmento de parâmetro ({...}).
    /// </summary>
    public static bool HasRouteParameters(string route) =>
        route.Contains('{') && route.Contains('}');
}
