using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScepAdmin.Models;

/// <summary>
/// Tracks the status of the most recent CRL generation.
/// </summary>
[Table("tblSCEPCrlStatus")]
public class CrlStatus
{
    [Key]
    /// <summary>Primary key.</summary>
    public int Id { get; set; }
    /// <summary>Last CRL generation timestamp (UTC).</summary>
    public DateTime? LastGeneratedAt { get; set; }
    /// <summary>Next scheduled CRL update (UTC).</summary>
    public DateTime? NextUpdateAt { get; set; }
    /// <summary>CRL version number.</summary>
    public int? Version { get; set; }
    /// <summary>True if a CRL is currently being generated.</summary>
    public bool? IsGenerating { get; set; }
}
