using System.Text.Json.Serialization;

namespace SignerServiceCore.Models;

public sealed class PipeRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Action { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
}

public sealed class PipeResponse
{
    public string Id { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? DataJson { get; set; }
    public string? Error { get; set; }
}

public sealed class ValidatePersIdRequest
{
    [JsonPropertyName("inputPersID")]
    public string? InputPersID { get; set; }
}
public sealed class ValidateThumbPrintRequest
{
    [JsonPropertyName("ThumbPrint")]
    public string? ThumbPrint { get; set; }
}

