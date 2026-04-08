using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

/// <summary>
/// Represents a managed device (certificate subject).
/// </summary>
[Table("tblSCEPDevices")]
public class Device
{
    [Key]
    /// <summary>Primary key.</summary>
    public int Id { get; set; }
    /// <summary>Foreign key to company.</summary>
    public int CompanyId { get; set; }
    /// <summary>Display name for the device.</summary>
    public string DeviceName { get; set; } = string.Empty;
    /// <summary>Unique identifier for the device (e.g. serial, CN).</summary>
    public string DeviceIdentifier { get; set; } = string.Empty;
    /// <summary>Last time the device was seen (UTC).</summary>
    public DateTime? LastSeenAt { get; set; }
    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Navigation property to company.</summary>
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;
    /// <summary>Navigation property to issued certificates.</summary>
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
