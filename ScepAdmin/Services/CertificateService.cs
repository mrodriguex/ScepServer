using System.Security.Cryptography.X509Certificates;


/// <summary>
/// Provides access to the CA certificate and related settings.
/// </summary>
namespace ScepAdmin.Services;

/// <summary>
/// Configuration settings for SCEP CA certificate.
/// </summary>
public class ScepSettings
{
    public string CaPath { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Interface for CA certificate access.
/// </summary>
public interface ICertificateService
{
    bool IsCaLoaded { get; }
    X509Certificate2? GetCaCertificate();
}

/// <summary>
/// Loads and provides the CA certificate for SCEP operations.
/// </summary>
public class CertificateService : ICertificateService
{
    private readonly ScepSettings _settings;
    private readonly ILogger<CertificateService> _logger;
    private X509Certificate2? _caCert;

    /// <summary>
    /// Loads CA certificate from configuration on construction.
    /// </summary>
    /// <param name="configuration">App configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public CertificateService(IConfiguration configuration, ILogger<CertificateService> logger)
    {
        _settings = configuration.GetSection("Scep").Get<ScepSettings>() ?? new ScepSettings();
        _logger = logger;
        LoadCertificate();
    }

    /// <summary>
    /// True if CA certificate is loaded.
    /// </summary>
    public bool IsCaLoaded => _caCert != null;

    /// <summary>
    /// Returns the loaded CA certificate, or null if not loaded.
    /// </summary>
    public X509Certificate2? GetCaCertificate() => _caCert;

    /// <summary>
    /// Loads the CA certificate from the configured path.
    /// </summary>
    private void LoadCertificate()
    {
        try
        {
            // Check if path is configured and file exists
            if (string.IsNullOrEmpty(_settings.CaPath) || !File.Exists(_settings.CaPath))
            {
                _logger.LogWarning("CA certificate path not configured or file not found: {Path}", _settings.CaPath);
                return;
            }
            // Load the CA certificate
            _caCert = new X509Certificate2(_settings.CaPath, _settings.Password);
            _logger.LogInformation("CA certificate loaded: {Subject}", _caCert.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load CA certificate from {Path}", _settings.CaPath);
        }
    }
}
