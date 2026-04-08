using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

/// <summary>
/// Represents an issued certificate (active or revoked).
/// </summary>
[Table("tblSCEPCertificates")]
public class Certificate
{
    [Key]
    /// <summary>Primary key.</summary>
    public int Id { get; set; }
    /// <summary>Foreign key to device.</summary>
    public int DeviceId { get; set; }
    /// <summary>Foreign key to company.</summary>
    public int CompanyId { get; set; }
    /// <summary>X.509 serial number (hex).</summary>
    public string SerialNumber { get; set; } = string.Empty;
    /// <summary>X.509 subject DN.</summary>
    public string Subject { get; set; } = string.Empty;
    /// <summary>Certificate validity start (UTC).</summary>
    public DateTime NotBefore { get; set; }
    /// <summary>Certificate validity end (UTC).</summary>
    public DateTime NotAfter { get; set; }
    /// <summary>True if revoked.</summary>
    public bool IsRevoked { get; set; }
    /// <summary>Revocation timestamp (UTC), if revoked.</summary>
    public DateTime? RevokedAt { get; set; }
    /// <summary>True if this is a renewal for the device.</summary>
    public bool IsRenewal { get; set; }
    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Navigation property to device.</summary>
    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;
    /// <summary>Navigation property to company.</summary>
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;
}
