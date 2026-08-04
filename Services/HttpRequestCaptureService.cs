using System.Text;
using Microsoft.AspNetCore.Http;
using Savio.MockServer.Models;
using System.Text.Json;

namespace Savio.MockServer.Services;

public static class HttpRequestCaptureService
{
    public sealed record CapturedRequest(
        string? TextBody,
        string? FormJson,
        string? BodyBase64,
        string BodyContentType,
        string? BodyFileName);

    public static async Task<CapturedRequest> CaptureAsync(HttpRequest request)
    {
        request.EnableBuffering();

        var requestContentType = request.ContentType ?? string.Empty;

        if (request.HasFormContentType)
        {
            byte[] rawMultipartBytes;
            using (var rawMs = new MemoryStream())
            {
                await request.Body.CopyToAsync(rawMs);
                rawMultipartBytes = rawMs.ToArray();
            }
            request.Body.Position = 0;

            var form = await request.ReadFormAsync();

            var payload = new MultipartPayload();

            foreach (var kv in form)
            {
                foreach (var value in kv.Value)
                {
                    payload.Fields.Add(new MultipartPayload.FormField { Name = kv.Key, Value = value ?? string.Empty });
                }
            }

            foreach (var file in form.Files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : (file.ContentType ?? "application/octet-stream");

                payload.Files.Add(new MultipartPayload.FormFilePart
                {
                    Name = file.Name,
                    FileName = file.FileName,
                    ContentType = contentType,
                    Length = file.Length
                });
            }

            request.Body.Position = 0;

            return new CapturedRequest(
                TextBody: null,
                FormJson: JsonSerializer.Serialize(payload),
                BodyBase64: rawMultipartBytes.Length > 0 ? Convert.ToBase64String(rawMultipartBytes) : null,
                BodyContentType: requestContentType,
                BodyFileName: null);
        }

        try
        {
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            request.Body.Position = 0;

            var bytes = ms.ToArray();

            if (bytes.Length == 0)
            {
                return new CapturedRequest(
                    TextBody: string.Empty,
                    FormJson: null,
                    BodyBase64: null,
                    BodyContentType: requestContentType,
                    BodyFileName: null);
            }

            if (IsTextBasedContentType(requestContentType) && TryDecodeUtf8(bytes, out var textBody))
            {
                return new CapturedRequest(
                    TextBody: textBody,
                    FormJson: null,
                    BodyBase64: null,
                    BodyContentType: requestContentType,
                    BodyFileName: null);
            }

            return new CapturedRequest(
                TextBody: null,
                FormJson: null,
                BodyBase64: Convert.ToBase64String(bytes),
                BodyContentType: requestContentType,
                BodyFileName: null);
        }
        catch
        {
            request.Body.Position = 0;
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            request.Body.Position = 0;

            return new CapturedRequest(
                TextBody: null,
                FormJson: null,
                BodyBase64: Convert.ToBase64String(ms.ToArray()),
                BodyContentType: requestContentType,
                BodyFileName: null);
        }
    }

    private static bool IsTextBasedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDecodeUtf8(byte[] bytes, out string text)
    {
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
            return true;
        }
        catch
        {
            text = string.Empty;
            return false;
        }
    }
}
