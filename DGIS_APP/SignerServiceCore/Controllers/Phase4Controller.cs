using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using SignerServiceCore.Infrastructure;
using SignerServiceCore.Models;

namespace SignerServiceCore.Controllers;

[ApiController]
[Route("Temporary_Listen_Addresses")]
public sealed class Phase4Controller : ControllerBase
{
    private readonly DgisPipeClient _pipeClient;
    private readonly IConfiguration _configuration;

    public Phase4Controller(DgisPipeClient pipeClient, IConfiguration configuration)
    {
        _pipeClient = pipeClient;
        _configuration = configuration;
    }

    [HttpGet("Phase4Status")]
    public async Task<IActionResult> Phase4Status(CancellationToken cancellationToken)
    {
        var pipe = await _pipeClient.SendAsync("Ping", null, cancellationToken);
        var section = _configuration.GetSection("DgisSigner");
        return Ok(new
        {
            status = pipe.Success ? "OK" : "DGIS_DESKTOP_NOT_CONNECTED",
            phase = 4,
            server = "ASP.NET Core Kestrel",
            httpSysRequired = false,
            wcfHttpHostRequired = false,
            namedPipeConnected = pipe.Success,
            allLegacyApiGroupsEnabled = pipe.Success,
            namedPipe = section["NamedPipe:Name"],
            hostName = section["HostName"],
            port = section.GetValue<int>("Port"),
            bindAddress = "127.0.0.1",
            desktopResponse = pipe.DataJson,
            error = pipe.Error
        });
    }

    [HttpGet("Phase3Status")]
    [HttpGet("Phase2Status")]
    public Task<IActionResult> PreviousPhaseStatus(CancellationToken cancellationToken) => Phase4Status(cancellationToken);

    [HttpGet("FetchPersID")]
    public Task<IActionResult> FetchPersID(CancellationToken ct) => JsonAction("FetchPersID", null, ct);
    [HttpGet("FetchTokenDetails")]
    public Task<IActionResult> FetchTokenDetails(CancellationToken ct) => JsonAction("FetchTokenDetails", null, ct);
    [HttpGet("FetchUniqueTokenDetails")]
    public Task<IActionResult> FetchUniqueTokenDetails(CancellationToken ct) => JsonAction("FetchUniqueTokenDetails", null, ct);
    [HttpGet("GetPublicKey")]
    public Task<IActionResult> GetPublicKey(CancellationToken ct) => JsonAction("GetPublicKey", null, ct);
    [HttpGet("HasInternetConnectionAsyncTest")]
    public Task<IActionResult> HasInternetConnectionAsyncTest(CancellationToken ct) => JsonAction("HasInternetConnectionAsyncTest", null, ct);
    [HttpGet("Getpdffile")]
    public Task<IActionResult> Getpdffile(CancellationToken ct) => JsonAction("Getpdffile", null, ct);

    [HttpGet("FetchTokenOCSPCrlDetails")]
    [HttpGet("FetchTokenOCSPCrlDetailsAsync")]
    public Task<IActionResult> FetchTokenOCSPCrlDetails(
        [FromQuery] bool IsCheckCrl = false,
        [FromQuery] string ThumbPrint = "",
        CancellationToken cancellationToken = default) =>
        JsonAction("FetchTokenOCSPCrlDetails", new { IsCheckCrl, ThumbPrint }, cancellationToken);

    [HttpPost("ValidatePersID")]
    public async Task<IActionResult> ValidatePersID([FromBody] ValidatePersIdRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InputPersID)) return Missing("inputPersID");
        var pipe = await _pipeClient.SendAsync("ValidatePersID", request.InputPersID.Trim(), ct);
        if (!pipe.Success) return PipeUnavailable(pipe);
        return Content($"{{\"ValidatePersIDResult\":{(string.IsNullOrWhiteSpace(pipe.DataJson) ? "[]" : pipe.DataJson)}}}", "application/json");
    }
    [HttpGet("FetchTokenOCSPDetails")]
    public Task<IActionResult> FetchTokenOCSPDetails(
        [FromQuery] string ThumbPrint = "",
        CancellationToken cancellationToken = default) =>
        JsonAction("FetchTokenOCSPDetails", new {  ThumbPrint }, cancellationToken);

    [HttpGet("FetchTokenCrlDetails")]
    public Task<IActionResult> FetchTokenCrlDetails(
        [FromQuery] string ThumbPrint = "",
        CancellationToken cancellationToken = default) =>
        JsonAction("FetchTokenCrlDetails", new {  ThumbPrint }, cancellationToken);

    [HttpPost("ValidatePersID2FA")]
    public async Task<IActionResult> ValidatePersID2FA([FromBody] ValidatePersIdRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InputPersID)) return Missing("inputPersID");
        var pipe = await _pipeClient.SendAsync("ValidatePersID2FA", request.InputPersID.Trim(), ct);
        if (!pipe.Success) return PipeUnavailable(pipe);
        return Content($"{{\"ValidatePersID2FAResult\":{(string.IsNullOrWhiteSpace(pipe.DataJson) ? "false" : pipe.DataJson)}}}", "application/json");
    }

    [HttpPost("SignXml")]
    [Consumes("application/xml", "text/xml", "text/plain")]
    [Produces("application/xml")]
    public async Task<IActionResult> SignXml(CancellationToken ct)
    {
        var xml = await ReadXmlBody(ct);
        if (xml.Error is not null) return BadRequest(new { error = xml.Error });
        var pipe = await _pipeClient.SendAsync("SignXml", xml.Xml, ct);
        if (!pipe.Success) return PipeUnavailable(pipe);
        var signedXml = DeserializeJsonString(pipe.DataJson);
        return Content(signedXml ?? "", "application/xml", Encoding.UTF8);
    }

    [HttpPost("VerifySignXml")]
    [Consumes("application/xml", "text/xml", "text/plain")]
    public async Task<IActionResult> VerifySignXml(CancellationToken ct)
    {
        var xml = await ReadXmlBody(ct);
        if (xml.Error is not null) return BadRequest(new { error = xml.Error });
        return PipeJson(await _pipeClient.SendAsync("VerifySignXml", xml.Xml, ct));
    }

    [HttpPost("SignHash")]
    public async Task<IActionResult> SignHash([FromBody] SignHashRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RData)) return Missing("rData");
        var pipe = await _pipeClient.SendAsync("SignHash", request.RData, ct);
        if (!pipe.Success) return PipeUnavailable(pipe);
        return Content($"{{\"SignHashResult\":{(string.IsNullOrWhiteSpace(pipe.DataJson) ? "null" : pipe.DataJson)}}}", "application/json");
    }

    [HttpPost("DigitalSignAsync")]
    public Task<IActionResult> DigitalSignAsync([FromBody] JsonElement request, CancellationToken ct) => JsonAction("DigitalSignAsync", request, ct);
    [HttpPost("DigitalSignBulkAsync")]
    public Task<IActionResult> DigitalSignBulkAsync([FromBody] JsonElement request, CancellationToken ct) => JsonAction("DigitalSignBulkAsync", request, ct);
    [HttpPost("ByteDigitalSignAsync")]
    public Task<IActionResult> ByteDigitalSignAsync([FromBody] JsonElement request, CancellationToken ct) => JsonAction("ByteDigitalSignAsync", request, ct);
    [HttpPost("DigitalSignVerifyAsync")]
    public Task<IActionResult> DigitalSignVerifyAsync([FromBody] JsonElement request, CancellationToken ct) => JsonAction("DigitalSignVerifyAsync", request, ct);
    [HttpPost("PdfCordinatefile")]
    public Task<IActionResult> PdfCordinatefile([FromBody] JsonElement request, CancellationToken ct) => JsonAction("PdfCordinatefile", request, ct);
    [HttpPost("AsymmetricEncryption")]
    public Task<IActionResult> AsymmetricEncryption([FromBody] JsonElement request, CancellationToken ct) => JsonAction("AsymmetricEncryption", request, ct);
    [HttpPost("AsymmetricDencryption")]
    [HttpPost("AsymmetricDecryption")]
    public Task<IActionResult> AsymmetricDencryption([FromBody] JsonElement request, CancellationToken ct) => JsonAction("AsymmetricDencryption", request, ct);
    [HttpPost("SymmetricEncryption")]
    public Task<IActionResult> SymmetricEncryption([FromBody] JsonElement request, CancellationToken ct) => JsonAction("SymmetricEncryption", request, ct);
    [HttpPost("SymmetricDencryption")]
    [HttpPost("SymmetricDecryption")]
    public Task<IActionResult> SymmetricDencryption([FromBody] JsonElement request, CancellationToken ct) => JsonAction("SymmetricDencryption", request, ct);
    [HttpPost("AddWaterMarks")]
    public Task<IActionResult> AddWaterMarks([FromBody] JsonElement request, CancellationToken ct) => JsonAction("AddWaterMarks", request, ct);

    private async Task<IActionResult> JsonAction(string action, object? payload, CancellationToken ct)
    {
        var pipe = await _pipeClient.SendAsync(action, payload, ct);
        return PipeJson(pipe);
    }

    private async Task<(string? Xml, string? Error)> ReadXmlBody(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 4096, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(body)) return (null, "XML request body is required.");
        try
        {
            var doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            doc.LoadXml(body);
            if (doc.DocumentElement is null) return (null, "XML root element is required.");
            return (body, null);
        }
        catch (XmlException ex) { return (null, "Invalid XML: " + ex.Message); }
    }

    private static string? DeserializeJsonString(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson) || dataJson == "null") return null;
        return JsonSerializer.Deserialize<string>(dataJson);
    }

    private IActionResult PipeJson(PipeResponse pipe)
    {
        if (!pipe.Success) return PipeUnavailable(pipe);
        return Content(string.IsNullOrWhiteSpace(pipe.DataJson) ? "null" : pipe.DataJson, "application/json");
    }

    private IActionResult PipeUnavailable(PipeResponse pipe) => StatusCode(StatusCodes.Status503ServiceUnavailable, new
    {
        status = 503,
        message = "DGIS desktop signer could not complete the request.",
        detail = pipe.Error
    });

    private IActionResult Missing(string field) => BadRequest(new { error = field + " is required." });

    public sealed class SignHashRequest { public string? RData { get; set; } }
}
