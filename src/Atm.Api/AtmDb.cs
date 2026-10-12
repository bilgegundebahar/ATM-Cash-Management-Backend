
using Microsoft.EntityFrameworkCore;

namespace Atm.Api;

public class AtmDb(DbContextOptions<AtmDb> options) : DbContext(options)
{
    public DbSet<AtmMachine> Atms => Set<AtmMachine>();
    public DbSet<Cassette> Cassettes => Set<Cassette>();
    public DbSet<DepositNote> DepositNotes => Set<DepositNote>();

    public DbSet<Reconciliation> Reconciliations
        => Set<Reconciliation>();

    public DbSet<ReconciliationItem> ReconciliationItems
        => Set<ReconciliationItem>();

    public DbSet<CassetteMovement> CassetteMovements
        => Set<CassetteMovement>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AtmMachine>()
            .Property(x => x.State)
            .HasConversion<string>();

        b.Entity<Cassette>()
            .Property(x => x.Kind)
            .HasConversion<string>();

        b.Entity<Cassette>()
            .Property(x => x.Currency)
            .HasConversion<string>();

        b.Entity<DepositNote>()
            .Property(x => x.Currency)
            .HasConversion<string>();

        b.Entity<AtmMachine>()
            .HasMany(x => x.Cassettes)
            .WithOne(x => x.AtmMachine)
            .HasForeignKey(x => x.AtmMachineId);

        b.Entity<Cassette>()
            .HasIndex(x => new { x.AtmMachineId, x.Slot })
            .IsUnique();

        b.Entity<DepositNote>()
            .HasIndex(x => new {
                x.CassetteId,
                x.Currency,
                x.Denomination
            })
            .IsUnique();

        b.Entity<Reconciliation>()
            .Property(x => x.Status)
            .HasConversion<string>();

        b.Entity<Reconciliation>()
            .HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.ReconciliationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CassetteMovement>()
            .Property(x => x.Currency)
            .HasConversion<string>();

        b.Entity<CassetteMovement>()
            .Property(x => x.Type)
            .HasConversion<string>();

        b.Entity<CassetteMovement>()
            .HasKey(x => x.Id);

        b.Entity<CassetteMovement>()
            .HasIndex(x => new { x.CassetteId, x.CreatedAt });
    }
}
