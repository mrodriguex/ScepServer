using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

[Table("tblSCEPIssuanceLog")]
public class IssuanceLog
{
    [Key]
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public int? DeviceId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
