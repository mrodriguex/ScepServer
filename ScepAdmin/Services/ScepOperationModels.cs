namespace ScepAdmin.Services;

/// <summary>
/// Represents a SCEP enrollment request for a device.
/// </summary>
public sealed class ScepEnrollmentRequest
{
    /// <summary>Target company/tenant ID.</summary>
    public int CompanyId { get; set; }
    /// <summary>Unique device identifier (e.g. serial, CN).</summary>
    public string DeviceIdentifier { get; set; } = string.Empty;
    /// <summary>Display name for the device.</summary>
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>X.509 subject DN for the certificate.</summary>
    public string Subject { get; set; } = string.Empty;
    /// <summary>SCEP challenge password.</summary>
    public string Challenge { get; set; } = string.Empty;
    /// <summary>Requested certificate validity in days.</summary>
    public int ValidityDays { get; set; } = 365;
}

/// <summary>
/// Result of a certificate issuance or revocation operation.
/// </summary>
public sealed class IssuanceResult
{
    /// <summary>True if the operation succeeded.</summary>
    public bool Success { get; set; }
    /// <summary>Human-readable status message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>ID of the issued certificate, if applicable.</summary>
    public int? CertificateId { get; set; }
    /// <summary>Serial number of the issued certificate, if applicable.</summary>
    public string? SerialNumber { get; set; }
}

/// <summary>
/// Result of a CRL (certificate revocation list) generation operation.
/// </summary>
public sealed class CrlGenerationResult
{
    /// <summary>True if CRL generation succeeded.</summary>
    public bool Success { get; set; }
    /// <summary>Human-readable status message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>CRL version number.</summary>
    public int Version { get; set; }
    /// <summary>Timestamp when CRL was generated (UTC).</summary>
    public DateTime GeneratedAt { get; set; }
    /// <summary>Timestamp for next scheduled CRL update (UTC).</summary>
    public DateTime NextUpdateAt { get; set; }
    /// <summary>Number of revoked certificates in the CRL.</summary>
    public int RevokedCount { get; set; }
    /// <summary>Base64-encoded CRL payload.</summary>
    public string PayloadBase64 { get; set; } = string.Empty;
}
