namespace Savio.MockServer.Models;

public sealed class MultipartResponse
{
    public string Subtype { get; set; } = "mixed"; // mixed|form-data|related
    public List<Part> Parts { get; set; } = new();

    public sealed class Part
    {
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Quando preenchido, o conteúdo da parte é texto (UTF-8).
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Opção antiga: conteúdo binário da parte em Base64.
        /// </summary>
        public string? Base64 { get; set; }

        /// <summary>
        /// Nova opção: referência a um blob persistido no banco.
        /// </summary>
        public int? BlobId { get; set; }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }
    }
}
