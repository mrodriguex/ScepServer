using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

[Table("tblSCEPCompany")]
public class Company
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChallengePassword { get; set; } = string.Empty;
    public string IntermediateCertPath { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
