using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

/// <summary>
/// Audit log entry for certificate issuance, revocation, or CRL generation.
/// </summary>
[Table("tblSCEPIssuanceLog")]
public class IssuanceLog
{
    [Key]
    /// <summary>Primary key.</summary>
    public int Id { get; set; }
    /// <summary>Related company (nullable).</summary>
    public int? CompanyId { get; set; }
    /// <summary>Related device (nullable).</summary>
    public int? DeviceId { get; set; }
    /// <summary>Operation type (Issue, Revoke, GenerateCRL, etc).</summary>
    public string Operation { get; set; } = string.Empty;
    /// <summary>Status (Success, Failed, etc).</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Human-readable message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Client IP address (or 'system').</summary>
    public string ClientIp { get; set; } = string.Empty;
    /// <summary>Timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
