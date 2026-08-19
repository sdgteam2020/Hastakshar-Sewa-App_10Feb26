using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using SignerServiceCore.Models;

namespace SignerServiceCore.Infrastructure;

public sealed class DgisPipeClient
{
    private readonly string _pipeName;
    private readonly int _connectTimeoutMs;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public DgisPipeClient(IConfiguration configuration)
    {
        var section = configuration.GetSection("DgisSigner:NamedPipe");
        _pipeName = section["Name"] ?? "DGISSignerPipeV1";
        _connectTimeoutMs = section.GetValue<int?>("ConnectTimeoutMs") ?? 5000;
    }

    public async Task<PipeResponse> SendAsync(
        string action,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var request = new PipeRequest
        {
            Id = Guid.NewGuid().ToString("N"),
            Action = action,
            PayloadJson = payload is null
                ? null
                : JsonSerializer.Serialize(payload, _jsonOptions)
        };

        using var client = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await client.ConnectAsync(_connectTimeoutMs, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            return Failed(request.Id,
                $"DGIS desktop pipe '{_pipeName}' was not available within {_connectTimeoutMs} ms. Start the patched DGISApp.exe first. {ex.Message}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed(request.Id,
                $"Timed out while connecting to DGIS desktop pipe '{_pipeName}'. Start the patched DGISApp.exe first.");
        }
        catch (Exception ex)
        {
            return Failed(request.Id,
                $"Unable to connect to DGIS desktop pipe '{_pipeName}': {ex.Message}");
        }

        try
        {
            using var writer = new StreamWriter(
                client,
                new UTF8Encoding(false),
                bufferSize: 4096,
                leaveOpen: true)
            {
                AutoFlush = true
            };

            using var reader = new StreamReader(
                client,
                new UTF8Encoding(false),
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true);

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            await writer.WriteLineAsync(requestJson);

            var responseLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseLine))
            {
                return Failed(request.Id, "DGIS desktop pipe returned an empty response.");
            }

            var response = JsonSerializer.Deserialize<PipeResponse>(responseLine, _jsonOptions);
            return response ?? Failed(request.Id, "DGIS desktop pipe returned an invalid response.");
        }
        catch (Exception ex)
        {
            return Failed(request.Id, $"Named Pipe request failed: {ex.Message}");
        }
    }

    private static PipeResponse Failed(string id, string message) => new()
    {
        Id = id,
        Success = false,
        Error = message
    };
}
