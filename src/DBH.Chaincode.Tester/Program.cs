using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.SingleLine = true;
});

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "CHAINCODE_TESTER_");

builder.Services.Configure<FabricOptions>(builder.Configuration.GetSection(FabricOptions.SectionName));
builder.Services.Configure<TestDataOptions>(builder.Configuration.GetSection(TestDataOptions.SectionName));
builder.Services.AddSingleton<IFabricGateway, FabricGatewayClient>();
builder.Services.AddSingleton<IEhrBlockchainService, EhrBlockchainService>();
builder.Services.AddSingleton<IConsentBlockchainService, ConsentBlockchainService>();
builder.Services.AddSingleton<IAuditBlockchainService, AuditBlockchainService>();
builder.Services.AddSingleton<ChaincodeTestRunner>();

var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
var runner = scope.ServiceProvider.GetRequiredService<ChaincodeTestRunner>();
await runner.RunAsync(args);

internal sealed class ChaincodeTestRunner
{
    private readonly IEhrBlockchainService _ehr;
    private readonly IConsentBlockchainService _consent;
    private readonly IAuditBlockchainService _audit;
    private readonly IFabricGateway _gateway;
    private readonly FabricOptions _fabric;
    private readonly ILogger<ChaincodeTestRunner> _logger;
    private readonly TestDataOptions _testData;

    public ChaincodeTestRunner(
        IEhrBlockchainService ehr,
        IConsentBlockchainService consent,
        IAuditBlockchainService audit,
        IFabricGateway gateway,
        IOptions<FabricOptions> fabricOptions,
        IOptions<TestDataOptions> testDataOptions,
        ILogger<ChaincodeTestRunner> logger)
    {
        _ehr = ehr;
        _consent = consent;
        _audit = audit;
        _gateway = gateway;
        _fabric = fabricOptions.Value;
        _testData = testDataOptions.Value;
        _logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        PrintHeader();

        var command = args.FirstOrDefault();
        var commandArgs = args.Skip(1).ToArray();

        if (string.IsNullOrWhiteSpace(command))
        {
            ShowCommands();
            command = Prompt("Pick command", "smoke");
        }

        try
        {
            switch (command.ToLowerInvariant())
            {
                case "help":
                    ShowCommands();
                    break;
                case "ping":
                    await CheckConnectionAsync();
                    break;
                case "smoke":
                    await RunSmokeAsync();
                    break;
                case "ehr-commit":
                    await CommitEhrAsync();
                    break;
                case "ehr-get":
                    await GetEhrAsync(commandArgs);
                    break;
                case "ehr-history":
                    await GetEhrHistoryAsync(commandArgs);
                    break;
                case "ehr-verify":
                    await VerifyEhrAsync(commandArgs);
                    break;
                case "consent-grant":
                    await GrantConsentAsync();
                    break;
                case "consent-revoke":
                    await RevokeConsentAsync(commandArgs);
                    break;
                case "consent-get":
                    await GetConsentAsync(commandArgs);
                    break;
                case "consent-verify":
                    await VerifyConsentAsync(commandArgs);
                    break;
                case "consent-patient":
                    await GetPatientConsentsAsync(commandArgs);
                    break;
                case "consent-history":
                    await GetConsentHistoryAsync(commandArgs);
                    break;
                case "audit-commit":
                    await CommitAuditAsync();
                    break;
                case "audit-get":
                    await GetAuditAsync(commandArgs);
                    break;
                case "audit-by-patient":
                    await GetAuditsByPatientAsync(commandArgs);
                    break;
                case "audit-by-actor":
                    await GetAuditsByActorAsync(commandArgs);
                    break;
                default:
                    Console.WriteLine($"Unknown command '{command}'. Type 'help'.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command '{Command}' failed", command);
        }
    }

    private void PrintHeader()
    {
        Console.WriteLine("=== DBH Chaincode Tester ===");
        Console.WriteLine($"Mode: {( _fabric.SimulationMode ? "Simulation" : "Real network" )}, Endpoint: {_fabric.PeerEndpoint}");
        Console.WriteLine("Use 'help' to list commands. Environment prefix: CHAINCODE_TESTER_");
        Console.WriteLine();
    }

    private void ShowCommands()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  ping               -> Check Fabric connectivity");
        Console.WriteLine("  smoke              -> Run sample flow (commit EHR, grant consent, audit)");
        Console.WriteLine("  ehr-commit         -> Commit sample EHR hash");
        Console.WriteLine("  ehr-get [id] [v]   -> Fetch EHR hash");
        Console.WriteLine("  ehr-history [id]   -> Fetch EHR history");
        Console.WriteLine("  ehr-verify [id] [v] [hash] -> Verify EHR hash");
        Console.WriteLine("  consent-grant      -> Grant sample consent");
        Console.WriteLine("  consent-revoke [id] [reason] -> Revoke consent");
        Console.WriteLine("  consent-get [id]   -> Get consent");
        Console.WriteLine("  consent-verify [id] [grantee] -> Verify consent");
        Console.WriteLine("  consent-patient [patientDid] -> List patient consents");
        Console.WriteLine("  consent-history [id] -> Consent history");
        Console.WriteLine("  audit-commit       -> Commit sample audit entry");
        Console.WriteLine("  audit-get [id]     -> Get audit entry");
        Console.WriteLine("  audit-by-patient [did] -> Audits by patient");
        Console.WriteLine("  audit-by-actor [did] -> Audits by actor");
        Console.WriteLine();
    }

    private async Task CheckConnectionAsync()
    {
        var connected = await _gateway.IsConnectedAsync();
        Console.WriteLine($"Fabric gateway connected: {connected}");
    }

    private async Task RunSmokeAsync()
    {
        Console.WriteLine("Running sample smoke test...");

        await CheckConnectionAsync();

        var ehr = BuildEhrRecord();
        Console.WriteLine($"[EHR] Commit {ehr.EhrId} v{ehr.Version}");
        var ehrResult = await _ehr.CommitEhrHashAsync(ehr);
        PrintTxResult(ehrResult);

        Console.WriteLine("[EHR] Fetching back...");
        var fetched = await _ehr.GetEhrHashAsync(ehr.EhrId, ehr.Version);
        Console.WriteLine(fetched == null ? "  Not found" : $"  Retrieved hash {fetched.ContentHash}");

        var consent = BuildConsentRecord();
        Console.WriteLine($"[CONSENT] Grant {consent.ConsentId} for {consent.GranteeDid}");
        var consentResult = await _consent.GrantConsentAsync(consent);
        PrintTxResult(consentResult);

        var verified = await _consent.VerifyConsentAsync(consent.ConsentId, consent.GranteeDid);
        Console.WriteLine($"[CONSENT] Verify result: {verified}");

        var audit = BuildAuditEntry();
        Console.WriteLine($"[AUDIT] Commit {audit.AuditId}");
        var auditResult = await _audit.CommitAuditEntryAsync(audit);
        PrintTxResult(auditResult);

        Console.WriteLine("Smoke test completed.");
    }

    private async Task CommitEhrAsync()
    {
        var record = BuildEhrRecord();
        Console.WriteLine($"Committing EHR hash: {record.EhrId} v{record.Version}");
        var result = await _ehr.CommitEhrHashAsync(record);
        PrintTxResult(result);
    }

    private async Task GetEhrAsync(IReadOnlyList<string> args)
    {
        var ehrId = args.Count > 0 ? args[0] : Prompt("EHR ID", _testData.Ehr.EhrId);
        var versionText = args.Count > 1 ? args[1] : Prompt("Version", _testData.Ehr.Version.ToString());
        var version = int.TryParse(versionText, out var parsed) ? parsed : 1;

        var record = await _ehr.GetEhrHashAsync(ehrId, version);
        Console.WriteLine(record == null ? "No record found" : JsonSerialize(record));
    }

    private async Task GetEhrHistoryAsync(IReadOnlyList<string> args)
    {
        var ehrId = args.Count > 0 ? args[0] : Prompt("EHR ID", _testData.Ehr.EhrId);
        var records = await _ehr.GetEhrHistoryAsync(ehrId);
        Console.WriteLine(records.Count == 0 ? "No history" : JsonSerialize(records));
    }

    private async Task VerifyEhrAsync(IReadOnlyList<string> args)
    {
        var ehrId = args.Count > 0 ? args[0] : Prompt("EHR ID", _testData.Ehr.EhrId);
        var versionText = args.Count > 1 ? args[1] : Prompt("Version", _testData.Ehr.Version.ToString());
        var currentHash = args.Count > 2 ? args[2] : Prompt("Current hash", _testData.Ehr.ContentHash);
        var version = int.TryParse(versionText, out var parsed) ? parsed : 1;

        var isValid = await _ehr.VerifyEhrIntegrityAsync(ehrId, version, currentHash);
        Console.WriteLine($"Integrity valid: {isValid}");
    }

    private async Task GrantConsentAsync()
    {
        var record = BuildConsentRecord();
        Console.WriteLine($"Granting consent {record.ConsentId} for {record.GranteeDid}");
        var result = await _consent.GrantConsentAsync(record);
        PrintTxResult(result);
    }

    private async Task RevokeConsentAsync(IReadOnlyList<string> args)
    {
        var consentId = args.Count > 0 ? args[0] : Prompt("Consent ID", _testData.Consent.ConsentId);
        var reason = args.Count > 1 ? args[1] : Prompt("Reason", "User requested");
        var revokedAt = DateTime.UtcNow.ToString("o");

        var result = await _consent.RevokeConsentAsync(consentId, revokedAt, reason);
        PrintTxResult(result);
    }

    private async Task GetConsentAsync(IReadOnlyList<string> args)
    {
        var consentId = args.Count > 0 ? args[0] : Prompt("Consent ID", _testData.Consent.ConsentId);
        var consent = await _consent.GetConsentAsync(consentId);
        Console.WriteLine(consent == null ? "No consent found" : JsonSerialize(consent));
    }

    private async Task VerifyConsentAsync(IReadOnlyList<string> args)
    {
        var consentId = args.Count > 0 ? args[0] : Prompt("Consent ID", _testData.Consent.ConsentId);
        var granteeDid = args.Count > 1 ? args[1] : Prompt("Grantee DID", _testData.Consent.GranteeDid);

        var valid = await _consent.VerifyConsentAsync(consentId, granteeDid);
        Console.WriteLine($"Consent valid: {valid}");
    }

    private async Task GetPatientConsentsAsync(IReadOnlyList<string> args)
    {
        var patientDid = args.Count > 0 ? args[0] : Prompt("Patient DID", _testData.Consent.PatientDid);
        var consents = await _consent.GetPatientConsentsAsync(patientDid);
        Console.WriteLine(consents.Count == 0 ? "No consents" : JsonSerialize(consents));
    }

    private async Task GetConsentHistoryAsync(IReadOnlyList<string> args)
    {
        var consentId = args.Count > 0 ? args[0] : Prompt("Consent ID", _testData.Consent.ConsentId);
        var history = await _consent.GetConsentHistoryAsync(consentId);
        Console.WriteLine(history.Count == 0 ? "No history" : JsonSerialize(history));
    }

    private async Task CommitAuditAsync()
    {
        var entry = BuildAuditEntry();
        Console.WriteLine($"Committing audit {entry.AuditId} for {entry.TargetType}:{entry.TargetId}");
        var result = await _audit.CommitAuditEntryAsync(entry);
        PrintTxResult(result);
    }

    private async Task GetAuditAsync(IReadOnlyList<string> args)
    {
        var auditId = args.Count > 0 ? args[0] : Prompt("Audit ID", _testData.Audit.AuditId);
        var audit = await _audit.GetAuditEntryAsync(auditId);
        Console.WriteLine(audit == null ? "No audit found" : JsonSerialize(audit));
    }

    private async Task GetAuditsByPatientAsync(IReadOnlyList<string> args)
    {
        var patientDid = args.Count > 0 ? args[0] : Prompt("Patient DID", _testData.Audit.PatientDid ?? _testData.Consent.PatientDid);
        var audits = await _audit.GetAuditsByPatientAsync(patientDid);
        Console.WriteLine(audits.Count == 0 ? "No audits" : JsonSerialize(audits));
    }

    private async Task GetAuditsByActorAsync(IReadOnlyList<string> args)
    {
        var actorDid = args.Count > 0 ? args[0] : Prompt("Actor DID", _testData.Audit.ActorDid);
        var audits = await _audit.GetAuditsByActorAsync(actorDid);
        Console.WriteLine(audits.Count == 0 ? "No audits" : JsonSerialize(audits));
    }

    private EhrHashRecord BuildEhrRecord()
    {
        var sample = _testData.Ehr;
        return new EhrHashRecord
        {
            EhrId = Use(sample.EhrId, $"ehr-{ShortGuid()}").Trim(),
            PatientDid = Use(sample.PatientDid, "did:demo:patient-001"),
            CreatedByDid = Use(sample.CreatedByDid, "did:demo:doctor-001"),
            OrganizationId = Use(sample.OrganizationId, "org-demo-001"),
            Version = sample.Version > 0 ? sample.Version : 1,
            ContentHash = Use(sample.ContentHash, Guid.NewGuid().ToString("N")),
            FileHash = Use(sample.FileHash, Guid.NewGuid().ToString("N")),
            Timestamp = Use(sample.Timestamp, DateTime.UtcNow.ToString("o")),
            EncryptedAesKey = Use(sample.EncryptedAesKey, "demo-key")
        };
    }

    private ConsentRecord BuildConsentRecord()
    {
        var sample = _testData.Consent;
        return new ConsentRecord
        {
            ConsentId = Use(sample.ConsentId, $"consent-{ShortGuid()}").Trim(),
            PatientDid = Use(sample.PatientDid, "did:demo:patient-001"),
            GranteeDid = Use(sample.GranteeDid, "did:demo:doctor-001"),
            GranteeType = Use(sample.GranteeType, "DOCTOR"),
            Permission = Use(sample.Permission, "READ"),
            Purpose = Use(sample.Purpose, "TREATMENT"),
            EhrId = Use(sample.EhrId, "ehr-demo-001"),
            GrantedAt = Use(sample.GrantedAt, DateTime.UtcNow.ToString("o")),
            ExpiresAt = string.IsNullOrWhiteSpace(sample.ExpiresAt) ? null : sample.ExpiresAt,
            Status = Use(sample.Status, "ACTIVE"),
            RevokedAt = sample.RevokedAt,
            RevokeReason = sample.RevokeReason,
            EncryptedAesKey = sample.EncryptedAesKey
        };
    }

    private AuditEntry BuildAuditEntry()
    {
        var sample = _testData.Audit;
        return new AuditEntry
        {
            AuditId = Use(sample.AuditId, $"audit-{ShortGuid()}").Trim(),
            ActorDid = Use(sample.ActorDid, "did:demo:doctor-001"),
            ActorType = Use(sample.ActorType, "DOCTOR"),
            Action = Use(sample.Action, "VIEW"),
            TargetType = Use(sample.TargetType, "EHR"),
            TargetId = Use(sample.TargetId, sample.ConsentId ?? "ehr-demo-001"),
            PatientDid = sample.PatientDid ?? _testData.Consent.PatientDid,
            ConsentId = sample.ConsentId ?? _testData.Consent.ConsentId,
            OrganizationId = sample.OrganizationId ?? _testData.Ehr.OrganizationId,
            Result = Use(sample.Result, "SUCCESS"),
            Timestamp = Use(sample.Timestamp, DateTime.UtcNow.ToString("o")),
            IpAddress = Use(sample.IpAddress, "127.0.0.1"),
            Metadata = sample.Metadata
        };
    }

    private static string Prompt(string label, string fallback)
    {
        Console.Write($"{label} [{fallback}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? fallback : input.Trim();
    }

    private static void PrintTxResult(BlockchainTransactionResult result)
    {
        if (result.Success)
        {
            Console.WriteLine($"  Success | TxHash={result.TxHash} | Block={result.BlockNumber} | At={result.Timestamp:o}");
            return;
        }

        var error = string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Unknown error" : result.ErrorMessage;
        Console.WriteLine($"  FAILED | {error}");
    }

    private static string Use(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string JsonSerialize(object value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string ShortGuid() => Guid.NewGuid().ToString("N").Substring(0, 8);
}

internal sealed class TestDataOptions
{
    public const string SectionName = "TestData";

    public EhrHashRecord Ehr { get; set; } = new();
    public ConsentRecord Consent { get; set; } = new();
    public AuditEntry Audit { get; set; } = new();
}
