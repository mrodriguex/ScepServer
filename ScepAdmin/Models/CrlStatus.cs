using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

[Table("tblSCEPCrlStatus")]
public class CrlStatus
{
    [Key]
    public int Id { get; set; }
    public DateTime LastGeneratedAt { get; set; }
    public DateTime NextUpdateAt { get; set; }
    public int Version { get; set; }
    public bool IsGenerating { get; set; }
}
