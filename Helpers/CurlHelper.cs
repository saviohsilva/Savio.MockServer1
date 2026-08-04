using System.Text;

namespace Savio.MockServer.Helpers;

/// <summary>Resultado da interpretação de um comando cURL colado pelo usuário.</summary>
public class CurlParseResult
{
    public string Method { get; set; } = "GET";
    public string Route { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public string Body { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
}

/// <summary>Conversão simplificada entre comandos cURL e a configuração de um mock (rota, método, headers e body).</summary>
public static class CurlHelper
{
    private static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public static CurlParseResult? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var tokens = Tokenize(input);
        if (tokens.Count == 0)
            return null;

        var startIndex = tokens[0].Equals("curl", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        var result = new CurlParseResult();
        string? url = null;
        string? explicitMethod = null;
        string? body = null;

        for (var i = startIndex; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token)
            {
                case "-X":
                case "--request":
                    if (i + 1 < tokens.Count)
                        explicitMethod = tokens[++i].ToUpperInvariant();
                    break;

                case "-H":
                case "--header":
                    if (i + 1 < tokens.Count)
                    {
                        var headerValue = tokens[++i];
                        var sep = headerValue.IndexOf(':');
                        if (sep > 0)
                        {
                            result.Headers[headerValue[..sep].Trim()] = headerValue[(sep + 1)..].Trim();
                        }
                        else
                        {
                            result.Warnings.Add($"Header inválido ignorado: \"{headerValue}\".");
                        }
                    }
                    break;

                case "-d":
                case "--data":
                case "--data-raw":
                case "--data-binary":
                case "--data-ascii":
                    if (i + 1 < tokens.Count)
                    {
                        var chunk = tokens[++i];
                        body = body == null ? chunk : body + "&" + chunk;
                    }
                    break;

                case "--url":
                    if (i + 1 < tokens.Count)
                        url = tokens[++i];
                    break;

                case "--data-urlencode":
                    if (i + 1 < tokens.Count)
                    {
                        body = (body == null ? string.Empty : body + "&") + tokens[++i];
                        result.Warnings.Add("--data-urlencode copiado como texto bruto; confira o encoding manualmente.");
                    }
                    break;

                case "-u":
                case "--user":
                    if (i + 1 < tokens.Count)
                        i++;
                    result.Warnings.Add("Credenciais (-u/--user) não são mapeadas automaticamente; configure a autenticação manualmente.");
                    break;

                case "-b":
                case "--cookie":
                    if (i + 1 < tokens.Count)
                        result.Headers["Cookie"] = tokens[++i];
                    break;

                case "-A":
                case "--user-agent":
                    if (i + 1 < tokens.Count)
                        result.Headers["User-Agent"] = tokens[++i];
                    break;

                case "-e":
                case "--referer":
                    if (i + 1 < tokens.Count)
                        result.Headers["Referer"] = tokens[++i];
                    break;

                case "-G":
                case "--get":
                    explicitMethod ??= "GET";
                    break;

                case "-k":
                case "--insecure":
                case "-L":
                case "--location":
                case "-s":
                case "--silent":
                case "-v":
                case "--verbose":
                case "-i":
                case "--include":
                case "--compressed":
                    // flags sem efeito na configuração do mock
                    break;

                default:
                    if (token.StartsWith('-'))
                    {
                        result.Warnings.Add($"Opção não reconhecida ignorada: \"{token}\".");
                    }
                    else if (url == null)
                    {
                        url = token;
                    }
                    else
                    {
                        result.Warnings.Add($"Argumento extra ignorado: \"{token}\".");
                    }
                    break;
            }
        }

        if (url == null)
        {
            result.Warnings.Add("Não foi possível localizar a URL no comando cURL.");
            return result;
        }

        var (route, hasQuery) = ExtractRoute(url);
        result.Route = route;
        if (hasQuery)
            result.Warnings.Add("Query string ignorada — rotas de mock não fazem correspondência por querystring.");

        result.Method = explicitMethod ?? (body != null ? "POST" : "GET");
        result.Body = body ?? string.Empty;

        if (!AllowedMethods.Contains(result.Method))
            result.Warnings.Add($"Método \"{result.Method}\" não é suportado; ajuste manualmente.");

        return result;
    }

    public static string BuildCurlCommand(string method, string url, IDictionary<string, string> headers, string? body)
    {
        var sb = new StringBuilder();
        sb.Append("curl -X ").Append(method.ToUpperInvariant()).Append(" \"").Append(url).Append('"');

        foreach (var header in headers)
        {
            sb.Append(" \\\n  -H \"").Append(EscapeForDoubleQuotes(header.Key)).Append(": ").Append(EscapeForDoubleQuotes(header.Value)).Append('"');
        }

        if (!string.IsNullOrEmpty(body))
        {
            sb.Append(" \\\n  -d '").Append(body.Replace("'", "'\\''")).Append('\'');
        }

        return sb.ToString();
    }

    private static string EscapeForDoubleQuotes(string value) => value.Replace("\"", "\\\"");

    private static (string route, bool hasQuery) ExtractRoute(string url)
    {
        var path = url;
        var schemeIdx = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeIdx >= 0)
        {
            var afterScheme = url[(schemeIdx + 3)..];
            var slashIdx = afterScheme.IndexOf('/');
            path = slashIdx >= 0 ? afterScheme[slashIdx..] : "/";
        }
        else if (!url.StartsWith('/'))
        {
            var slashIdx = url.IndexOf('/');
            path = slashIdx >= 0 ? url[slashIdx..] : "/";
        }

        var queryIdx = path.IndexOf('?');
        var hasQuery = queryIdx >= 0;
        if (hasQuery)
            path = path[..queryIdx];

        if (string.IsNullOrEmpty(path))
            path = "/";

        return (path, hasQuery);
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        char? quoteChar = null;

        var normalized = input
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("\\\n", " ")
            .Replace("^\n", " ");

        var i = 0;
        while (i < normalized.Length)
        {
            var c = normalized[i];

            if (quoteChar.HasValue)
            {
                if (c == '\\' && quoteChar == '"' && i + 1 < normalized.Length && (normalized[i + 1] == '"' || normalized[i + 1] == '\\'))
                {
                    sb.Append(normalized[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == quoteChar.Value)
                {
                    quoteChar = null;
                    i++;
                    continue;
                }

                sb.Append(c);
                i++;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                quoteChar = c;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
                i++;
                continue;
            }

            sb.Append(c);
            i++;
        }

        if (sb.Length > 0)
            tokens.Add(sb.ToString());

        return tokens;
    }
}
