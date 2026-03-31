using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

[Table("tblSCEPCertificatesArchive")]
public class CertificateArchive
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
    public DateTime ArchivedAt { get; set; }
}
