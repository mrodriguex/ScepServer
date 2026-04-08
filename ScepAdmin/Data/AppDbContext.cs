using Microsoft.EntityFrameworkCore;
using ScepAdmin.Models;

namespace ScepAdmin.Data;

/// <summary>
/// Entity Framework Core database context for the SCEP Admin application.
/// Configures all entity mappings and relationships.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Companies (tenants) table.</summary>
    public DbSet<Company> Companies => Set<Company>();
    /// <summary>Devices table.</summary>
    public DbSet<Device> Devices => Set<Device>();
    /// <summary>Issued certificates table.</summary>
    public DbSet<Certificate> Certificates => Set<Certificate>();
    /// <summary>Archived certificates table.</summary>
    public DbSet<CertificateArchive> CertificateArchives => Set<CertificateArchive>();
    /// <summary>Issuance and revocation audit log table.</summary>
    public DbSet<IssuanceLog> IssuanceLogs => Set<IssuanceLog>();
    /// <summary>CRL status tracking table.</summary>
    public DbSet<CrlStatus> CrlStatuses => Set<CrlStatus>();

    /// <summary>
    /// Configures the EF Core model and entity relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company entity configuration
        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("tblSCEPCompany");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.ChallengePassword).HasMaxLength(500);
            e.Property(x => x.IntermediateCertPath).HasMaxLength(1000);
            // Company has many devices and certificates
            e.HasMany(x => x.Devices).WithOne(d => d.Company).HasForeignKey(d => d.CompanyId);
            e.HasMany(x => x.Certificates).WithOne(c => c.Company).HasForeignKey(c => c.CompanyId);
        });

        // Device entity configuration
        modelBuilder.Entity<Device>(e =>
        {
            e.ToTable("tblSCEPDevices");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeviceName).HasMaxLength(500).IsRequired();
            e.Property(x => x.DeviceIdentifier).HasMaxLength(500);
            // Device belongs to a company
            e.HasOne(x => x.Company).WithMany(c => c.Devices).HasForeignKey(x => x.CompanyId);
        });

        // Certificate entity configuration
        modelBuilder.Entity<Certificate>(e =>
        {
            e.ToTable("tblSCEPCertificates");
            e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(200);
            e.Property(x => x.Subject).HasMaxLength(500);
            // Certificate belongs to a device and a company
            e.HasOne(x => x.Device).WithMany(d => d.Certificates).HasForeignKey(x => x.DeviceId);
            e.HasOne(x => x.Company).WithMany(c => c.Certificates).HasForeignKey(x => x.CompanyId);
        });

        // CertificateArchive entity configuration
        modelBuilder.Entity<CertificateArchive>(e =>
        {
            e.ToTable("tblSCEPCertificatesArchive");
            e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(200);
            e.Property(x => x.Subject).HasMaxLength(500);
        });

        // IssuanceLog entity configuration
        modelBuilder.Entity<IssuanceLog>(e =>
        {
            e.ToTable("tblSCEPIssuanceLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Operation).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Message).HasColumnType("nvarchar(max)");
            e.Property(x => x.ClientIp).HasMaxLength(100);
        });

        // CrlStatus entity configuration
        modelBuilder.Entity<CrlStatus>(e =>
        {
            e.ToTable("tblSCEPCrlStatus");
            e.HasKey(x => x.Id);
        });
    }
}
