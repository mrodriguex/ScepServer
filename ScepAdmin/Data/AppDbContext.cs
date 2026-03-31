using Microsoft.EntityFrameworkCore;
using ScepAdmin.Models;

namespace ScepAdmin.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CertificateArchive> CertificateArchives => Set<CertificateArchive>();
    public DbSet<IssuanceLog> IssuanceLogs => Set<IssuanceLog>();
    public DbSet<CrlStatus> CrlStatuses => Set<CrlStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("tblSCEPCompany");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.ChallengePassword).HasMaxLength(500);
            e.Property(x => x.IntermediateCertPath).HasMaxLength(1000);
            e.HasMany(x => x.Devices).WithOne(d => d.Company).HasForeignKey(d => d.CompanyId);
            e.HasMany(x => x.Certificates).WithOne(c => c.Company).HasForeignKey(c => c.CompanyId);
        });

        modelBuilder.Entity<Device>(e =>
        {
            e.ToTable("tblSCEPDevices");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeviceName).HasMaxLength(500).IsRequired();
            e.Property(x => x.DeviceIdentifier).HasMaxLength(500);
            e.HasOne(x => x.Company).WithMany(c => c.Devices).HasForeignKey(x => x.CompanyId);
        });

        modelBuilder.Entity<Certificate>(e =>
        {
            e.ToTable("tblSCEPCertificates");
            e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(200);
            e.Property(x => x.Subject).HasMaxLength(500);
            e.HasOne(x => x.Device).WithMany(d => d.Certificates).HasForeignKey(x => x.DeviceId);
            e.HasOne(x => x.Company).WithMany(c => c.Certificates).HasForeignKey(x => x.CompanyId);
        });

        modelBuilder.Entity<CertificateArchive>(e =>
        {
            e.ToTable("tblSCEPCertificatesArchive");
            e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(200);
            e.Property(x => x.Subject).HasMaxLength(500);
        });

        modelBuilder.Entity<IssuanceLog>(e =>
        {
            e.ToTable("tblSCEPIssuanceLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Operation).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.Message).HasColumnType("nvarchar(max)");
            e.Property(x => x.ClientIp).HasMaxLength(100);
        });

        modelBuilder.Entity<CrlStatus>(e =>
        {
            e.ToTable("tblSCEPCrlStatus");
            e.HasKey(x => x.Id);
        });
    }
}
