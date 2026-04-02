using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

[Table("tblSCEPCertificates")]
public class Certificate
{
    [Key]
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int CompanyId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRenewal { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;
    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;
}
