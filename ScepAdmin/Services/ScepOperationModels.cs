namespace ScepAdmin.Services;

public sealed class ScepEnrollmentRequest
{
    public int CompanyId { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 365;
}

public sealed class IssuanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? CertificateId { get; set; }
    public string? SerialNumber { get; set; }
}

public sealed class CrlGenerationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime NextUpdateAt { get; set; }
    public int RevokedCount { get; set; }
    public string PayloadBase64 { get; set; } = string.Empty;
}
