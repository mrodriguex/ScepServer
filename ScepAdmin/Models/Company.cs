using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

/// <summary>
/// Represents a customer/company/tenant in the SCEP system.
/// </summary>
[Table("tblSCEPCompany")]
public class Company
{
    [Key]
    /// <summary>Primary key.</summary>
    public int Id { get; set; }
    /// <summary>Company display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>SCEP challenge password for this company.</summary>
    public string ChallengePassword { get; set; } = string.Empty;
    /// <summary>Path to intermediate certificate (optional).</summary>
    public string IntermediateCertPath { get; set; } = string.Empty;
    /// <summary>True if company is active/enabled.</summary>
    public bool IsActive { get; set; }
    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Navigation property to devices.</summary>
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    /// <summary>Navigation property to certificates.</summary>
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
