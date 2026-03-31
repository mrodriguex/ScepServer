using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

[Table("tblSCEPDevices")]
public class Device
{
    [Key]
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
