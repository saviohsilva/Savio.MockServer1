using System.Text;
using System.Text.Json;
using Savio.MockServer.Models;

namespace Savio.MockServer.Services;

public static class MultipartResponseWriter
{
    public static async Task<bool> TryWriteMultipartAsync(HttpContext context, string? multipartJson, IMockBinaryStorage binaryStorage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(multipartJson))
        {
            return false;
        }

        MultipartResponse? multi;
        try
        {
            multi = JsonSerializer.Deserialize<MultipartResponse>(multipartJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return false;
        }

        if (multi == null || multi.Parts.Count == 0)
        {
            return false;
        }

        var boundary = "----SavioMockBoundary" + Guid.NewGuid().ToString("N");
        var contentType = $"multipart/{(string.IsNullOrWhiteSpace(multi.Subtype) ? "mixed" : multi.Subtype)}; boundary={boundary}";

        context.Response.ContentType = contentType;

        foreach (var part in multi.Parts)
        {
            await context.Response.WriteAsync($"--{boundary}\r\n", Encoding.UTF8, cancellationToken);

            var hasContentType = false;
            foreach (var h in part.Headers)
            {
                if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    hasContentType = true;
                }

                await context.Response.WriteAsync($"{h.Key}: {h.Value}\r\n", Encoding.UTF8, cancellationToken);
            }

            var partContentType = part.ContentType;
            if (!hasContentType && !string.IsNullOrWhiteSpace(partContentType))
            {
                await context.Response.WriteAsync($"Content-Type: {partContentType}\r\n", Encoding.UTF8, cancellationToken);
                hasContentType = true;
            }

            if (!string.IsNullOrWhiteSpace(part.FileName))
            {
                await context.Response.WriteAsync($"Content-Disposition: attachment; filename=\"{part.FileName}\"\r\n", Encoding.UTF8, cancellationToken);
            }

            if (!hasContentType)
            {
                await context.Response.WriteAsync("Content-Type: text/plain; charset=utf-8\r\n", Encoding.UTF8, cancellationToken);
            }

            await context.Response.WriteAsync("\r\n", Encoding.UTF8, cancellationToken);

            if (part.BlobId.HasValue)
            {
                var blob = await binaryStorage.GetAsync(part.BlobId.Value, cancellationToken);
                if (blob != null)
                {
                    await context.Response.Body.WriteAsync(blob.Value.bytes, cancellationToken);
                }
                await context.Response.WriteAsync("\r\n", Encoding.UTF8, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(part.Base64))
            {
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(part.Base64);
                }
                catch
                {
                    bytes = Array.Empty<byte>();
                }

                await context.Response.Body.WriteAsync(bytes, cancellationToken);
                await context.Response.WriteAsync("\r\n", Encoding.UTF8, cancellationToken);
            }
            else
            {
                await context.Response.WriteAsync((part.Text ?? string.Empty) + "\r\n", Encoding.UTF8, cancellationToken);
            }
        }

        await context.Response.WriteAsync($"--{boundary}--\r\n", Encoding.UTF8, cancellationToken);
        return true;
    }
}
