using System.Text.Json.Serialization;

namespace Savio.MockServer.Models;

public sealed class MultipartPayload
{
    public List<FormField> Fields { get; set; } = new();
    public List<FormFilePart> Files { get; set; } = new();

    public sealed class FormField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public sealed class FormFilePart
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long Length { get; set; }

        /// <summary>
        /// Conteúdo em Base64 quando persistido/transportado.
        /// Evita depender de encoding de texto para binários.
        /// </summary>
        public string? Base64 { get; set; }

        [JsonIgnore]
        public byte[]? Bytes { get; set; }
    }
}
