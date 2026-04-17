using System.Text.Encodings.Web;

namespace Savio.MockServer.Helpers;

public static class UiHelpers
{
    public static string GetMethodColor(string method) => method.ToUpper() switch
    {
        "GET" => "primary",
        "POST" => "success",
        "PUT" => "warning",
        "DELETE" => "danger",
        "PATCH" => "info",
        _ => "secondary"
    };

    public static string GetStatusColor(int status) => status switch
    {
        >= 200 and < 300 => "success",
        >= 300 and < 400 => "info",
        >= 400 and < 500 => "warning",
        >= 500 => "danger",
        _ => "secondary"
    };

    public static string FormatJson(string json)
    {
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json,
                new System.Text.Json.JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = System.Text.Json.JsonCommentHandling.Skip
                });
            return System.Text.Json.JsonSerializer.Serialize(jsonDoc,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
        }
        catch
        {
            return json;
        }
    }
}
