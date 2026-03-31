using System.Security.Cryptography.X509Certificates;

namespace ScepAdmin.Services;

public class ScepSettings
{
    public string CaPath { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public interface ICertificateService
{
    bool IsCaLoaded { get; }
    X509Certificate2? GetCaCertificate();
}

public class CertificateService : ICertificateService
{
    private readonly ScepSettings _settings;
    private readonly ILogger<CertificateService> _logger;
    private X509Certificate2? _caCert;

    public CertificateService(IConfiguration configuration, ILogger<CertificateService> logger)
    {
        _settings = configuration.GetSection("Scep").Get<ScepSettings>() ?? new ScepSettings();
        _logger = logger;
        LoadCertificate();
    }

    public bool IsCaLoaded => _caCert != null;

    public X509Certificate2? GetCaCertificate() => _caCert;

    private void LoadCertificate()
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.CaPath) || !File.Exists(_settings.CaPath))
            {
                _logger.LogWarning("CA certificate path not configured or file not found: {Path}", _settings.CaPath);
                return;
            }
            _caCert = new X509Certificate2(_settings.CaPath, _settings.Password);
            _logger.LogInformation("CA certificate loaded: {Subject}", _caCert.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load CA certificate from {Path}", _settings.CaPath);
        }
    }
}
