using System.Net;
using SignerServiceCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<DgisPipeClient>();

var signerSection = builder.Configuration.GetSection("DgisSigner");
var port = signerSection.GetValue<int?>("Port") ?? 55102;
var hostName = signerSection.GetValue<string>("HostName") ?? "dgisapp.army.mil";
var certificatePath = signerSection["Certificate:Path"] ?? "Certificates/dgisapp.pfx";
var certificatePassword = signerSection["Certificate:Password"] ?? string.Empty;
if (!Path.IsPathRooted(certificatePath)) certificatePath = Path.Combine(builder.Environment.ContentRootPath, certificatePath);
if (!File.Exists(certificatePath)) throw new FileNotFoundException($"DGIS HTTPS certificate was not found at '{certificatePath}'.", certificatePath);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, port, listenOptions => listenOptions.UseHttps(certificatePath, certificatePassword));
});

var allowedOrigins = signerSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddPolicy("DgisBrowserClients", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS")));
}

var app = builder.Build();
if (allowedOrigins.Length > 0) app.UseCors("DgisBrowserClients");
app.MapControllers();

app.MapGet("/", () => Results.Content($$$"""
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>DGIS Signer - Phase 4</title>
<style>body{font-family:Segoe UI,Arial;max-width:1100px;margin:24px auto;padding:0 18px;color:#202124}button,select,input{padding:8px;margin:4px}textarea{width:100%;min-height:210px;font-family:Consolas,monospace}pre{background:#f5f5f5;border:1px solid #ddd;padding:12px;min-height:150px;white-space:pre-wrap;overflow:auto}.note{padding:10px;border:1px solid #b9d7f5;background:#eef6ff}.warn{padding:10px;border:1px solid #e3c65c;background:#fff8df;margin-top:8px}code{background:#eee;padding:2px 4px}</style>
</head><body>
<h1>DGIS Signer - Phase 4</h1><p><code>https://{{hostName}}:{{port}}</code></p>
<div class="note">All legacy API groups now use <b>Kestrel → DGISSignerPipeV1 → existing Service1</b>. Start the Phase-4-patched DGISApp.exe first.</div>
<div class="warn"><b>Use TEST COPIES only</b> for signing, encryption, decryption and watermark APIs. Some existing Service1 methods process every file in a supplied folder.</div>
<h3>Connection / safe GET tests</h3>
<button onclick="getApi('Phase4Status')">Phase4Status</button><button onclick="getApi('FetchPersID')">FetchPersID</button><button onclick="getApi('FetchTokenDetails')">FetchTokenDetails</button><button onclick="getApi('FetchUniqueTokenDetails')">FetchUniqueTokenDetails</button><button onclick="getApi('GetPublicKey')">GetPublicKey</button><button onclick="getApi('HasInternetConnectionAsyncTest')">Internet Test</button>
<h3>CRL / OCSP</h3><label><input id="crl" type="checkbox">Check CRL/OCSP</label><input id="thumb" placeholder="ThumbPrint (optional)" size="48"><button onclick="crl()">FetchTokenOCSPCrlDetails</button>
<h3>OCSP</h3><input id="thumbocsp" placeholder="ThumbPrint (optional)" size="48"><button onclick="ocsp()">FetchTokenOCSPDetails</button>
<h3>CRL</h3><input id="thumbonlycrl" placeholder="ThumbPrint (optional)" size="48"><button onclick="crlonly()">FetchTokenCRLDetails</button>
<h3>XML sign / verify</h3><textarea id="xml">&lt;Root&gt;&lt;Message&gt;Phase 4 XML test&lt;/Message&gt;&lt;/Root&gt;</textarea><br><button onclick="xmlCall('SignXml')">SignXml</button><button onclick="xmlCall('VerifySignXml')">VerifySignXml</button>
<h3>JSON API runner</h3>
<select id="op"><option>ValidatePersID</option><option>ValidatePersID2FA</option><option>SignHash</option><option>DigitalSignAsync</option><option>DigitalSignBulkAsync</option><option>ByteDigitalSignAsync</option><option>DigitalSignVerifyAsync</option><option>PdfCordinatefile</option><option>AsymmetricEncryption</option><option>AsymmetricDencryption</option><option>SymmetricEncryption</option><option>SymmetricDencryption</option><option>AddWaterMarks</option></select>
<textarea id="json">{ "inputPersID": "YOUR_PERSONAL_NUMBER" }</textarea><br><button onclick="postJson()">Run selected POST</button>
<h3>Result</h3><pre id="result">Click Phase4Status first.</pre>
<script>
const r=document.getElementById('result'); const base='/Temporary_Listen_Addresses/';
async function show(x){const t=await x.text();let d=t;try{d=JSON.stringify(JSON.parse(t),null,2)}catch{}r.textContent='HTTP '+x.status+'\n'+d}
async function getApi(op){r.textContent='Calling '+op+'...';try{await show(await fetch(base+op))}catch(e){r.textContent=e}}
async function crl(){const q='?IsCheckCrl='+document.getElementById('crl').checked+'&ThumbPrint='+encodeURIComponent(document.getElementById('thumb').value);try{await show(await fetch(base+'FetchTokenOCSPCrlDetails'+q))}catch(e){r.textContent=e}}
async function ocsp(){const q='?ThumbPrint='+encodeURIComponent(document.getElementById('thumbocsp').value);try{await show(await fetch(base+'FetchTokenOCSPDetails'+q))}catch(e){r.textContent=e}}
async function crlonly(){const q='?ThumbPrint='+encodeURIComponent(document.getElementById('thumbonlycrl').value);try{await show(await fetch(base+'FetchTokenCRLDetails'+q))}catch(e){r.textContent=e}}
async function xmlCall(op){if(op==='SignXml'&&!confirm('SignXml may invoke the token/private key. Continue?'))return;try{await show(await fetch(base+op,{method:'POST',headers:{'Content-Type':'application/xml'},body:document.getElementById('xml').value}))}catch(e){r.textContent=e}}
async function postJson(){const op=document.getElementById('op').value;let body;try{body=JSON.parse(document.getElementById('json').value)}catch(e){r.textContent='Invalid JSON: '+e;return}const risky=['DigitalSignAsync','DigitalSignBulkAsync','ByteDigitalSignAsync','AsymmetricEncryption','AsymmetricDencryption','SymmetricEncryption','SymmetricDencryption','AddWaterMarks','SignHash','ValidatePersID2FA'];if(risky.includes(op)&&!confirm(op+' invokes sensitive/token/file functionality. Use TEST data only. Continue?'))return;try{await show(await fetch(base+op,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}))}catch(e){r.textContent=e}}
</script></body></html>
""", "text/html"));

app.Logger.LogInformation("DGIS Phase 4 Kestrel host listening on https://{HostName}:{Port}; all migrated API groups forward through the local Named Pipe.", hostName, port);
app.Run();
