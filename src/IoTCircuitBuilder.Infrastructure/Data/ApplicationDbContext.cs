using IoTCircuitBuilder.Domain.Entities;
using IoTCircuitBuilder.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IoTCircuitBuilder.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Pin> Pins => Set<Pin>();
    public DbSet<PinCapability> PinCapabilities => Set<PinCapability>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<ComponentPinRequirement> ComponentPinRequirements => Set<ComponentPinRequirement>();
    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<ComponentLibrary> ComponentLibraries => Set<ComponentLibrary>();
    public DbSet<I2cAddress> I2cAddresses => Set<I2cAddress>();
    public DbSet<CodeTemplate> CodeTemplates => Set<CodeTemplate>();
    public DbSet<PowerDistributionRule> PowerDistributionRules => Set<PowerDistributionRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── Board ──────────────────────────────────────────────
        modelBuilder.Entity<Board>(e =>
        {
            e.HasKey(b => b.BoardId);
            e.HasIndex(b => b.Name).IsUnique();
            e.Property(b => b.MaxCurrentMa).IsRequired();
            e.Property(b => b.Voltage).HasPrecision(4, 2);
            e.Property(b => b.LogicLevelV).HasPrecision(4, 2);
        });

        // ─── Pin ────────────────────────────────────────────────
        modelBuilder.Entity<Pin>(e =>
        {
            e.HasKey(p => p.PinId);
            e.HasIndex(p => new { p.BoardId, p.PinIdentifier }).IsUnique();
            e.Property(p => p.Voltage).HasPrecision(4, 2);
            e.HasOne(p => p.Board).WithMany(b => b.Pins).HasForeignKey(p => p.BoardId).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── PinCapability ──────────────────────────────────────
        modelBuilder.Entity<PinCapability>(e =>
        {
            e.HasKey(pc => pc.CapabilityId);
            e.HasIndex(pc => new { pc.PinId, pc.CapabilityType }).IsUnique();
            e.Property(pc => pc.CapabilityType).HasConversion<string>();
            e.HasOne(pc => pc.Pin).WithMany(p => p.Capabilities).HasForeignKey(pc => pc.PinId).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Component ──────────────────────────────────────────
        modelBuilder.Entity<Component>(e =>
        {
            e.HasKey(c => c.ComponentId);
            e.HasIndex(c => c.Type).IsUnique();
            e.Property(c => c.VoltageMin).HasPrecision(4, 2);
            e.Property(c => c.VoltageMax).HasPrecision(4, 2);
        });

        // ─── ComponentPinRequirement ─────────────────────────────
        modelBuilder.Entity<ComponentPinRequirement>(e =>
        {
            e.HasKey(r => r.RequirementId);
            e.HasIndex(r => new { r.ComponentId, r.PinName }).IsUnique();
            e.Property(r => r.RequiredCapability).HasConversion<string>();
            e.HasOne(r => r.Component).WithMany(c => c.PinRequirements).HasForeignKey(r => r.ComponentId).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Library ─────────────────────────────────────────────
        modelBuilder.Entity<Library>(e =>
        {
            e.HasKey(l => l.LibraryId);
            e.HasIndex(l => new { l.Name, l.Version }).IsUnique();
        });

        // ─── ComponentLibrary ────────────────────────────────────
        modelBuilder.Entity<ComponentLibrary>(e =>
        {
            e.HasKey(cl => cl.ComponentLibraryId);
            e.HasIndex(cl => new { cl.ComponentId, cl.LibraryId }).IsUnique();
            e.HasOne(cl => cl.Component).WithMany(c => c.ComponentLibraries).HasForeignKey(cl => cl.ComponentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cl => cl.Library).WithMany(l => l.ComponentLibraries).HasForeignKey(cl => cl.LibraryId).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── I2cAddress ──────────────────────────────────────────
        modelBuilder.Entity<I2cAddress>(e =>
        {
            e.HasKey(i => i.I2cAddressId);
            e.HasOne(i => i.Component).WithMany(c => c.I2cAddresses).HasForeignKey(i => i.ComponentId);
        });

        // ─── CodeTemplate ────────────────────────────────────────
        modelBuilder.Entity<CodeTemplate>(e =>
        {
            e.HasKey(t => t.TemplateId);
            e.HasOne(t => t.Component).WithMany(c => c.CodeTemplates).HasForeignKey(t => t.ComponentId).OnDelete(DeleteBehavior.Cascade);
        });

        // ─── PowerDistributionRule ───────────────────────────────
        modelBuilder.Entity<PowerDistributionRule>(e =>
        {
            e.HasKey(r => r.RuleId);
            e.Property(r => r.VoltageV).HasPrecision(4, 2);
            e.HasOne(r => r.Board).WithMany(b => b.PowerDistributionRules).HasForeignKey(r => r.BoardId).OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════════════════════════════════════════════════════════
        //  SEED DATA
        // ═══════════════════════════════════════════════════════════
        SeedData.Seed(modelBuilder);
    }
}
