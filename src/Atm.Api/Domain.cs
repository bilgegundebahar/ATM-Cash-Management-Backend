namespace Atm.Api;
public enum AtmState { UNCONFIGURED, CONFIGURED, LOADED, IN_SERVICE, MAINTENANCE }
public enum CassetteKind { Deposit, Recycling }
public enum Currency { TRY, USD, EUR }
public class AtmMachine {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public AtmState State { get; set; } = AtmState.UNCONFIGURED;
    public List<Cassette> Cassettes { get; set; } = new();
}
public class Cassette {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AtmMachineId { get; set; }
    public AtmMachine AtmMachine { get; set; } = null!;
    public int Slot { get; set; }
    public CassetteKind Kind { get; set; }
    public Currency? Currency { get; set; }
    public int? Denomination { get; set; }
    public int Count { get; set; }
    public List<DepositNote> DepositNotes { get; set; } = new();
}
public class DepositNote {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CassetteId { get; set; }
    public Cassette Cassette { get; set; } = null!;
    public Currency Currency { get; set; }
    public int Denomination { get; set; }
    public int Count { get; set; }
}
public record NoteAmount(int Denomination, int Count);
public record CassetteConfiguration(int Slot, CassetteKind Kind, Currency? Currency, int? Denomination);
public record CashLoad(int Slot, int Count);
public record NoteInput(Currency Currency, int Denomination, int Count);
public record WithdrawalRequest(Currency Currency, int Amount, int? DailyRemainingLimit = null);
public record WithdrawalChoice(int Amount, List<NoteAmount> Notes);
public record WithdrawalOptions(WithdrawalChoice? Exact, WithdrawalChoice? Lower, WithdrawalChoice? Upper);
public record DepositAllocation(int Slot, Currency Currency, int Denomination, int Count);
public record DepositRequest(List<NoteInput> Notes, int UnrecognisedCount);


public enum ReconciliationStatus
{
    Completed,
    AwaitingApproval,
    Approved
}

public class Reconciliation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AtmMachineId { get; set; }
    public string CreatedBy { get; set; } = "";
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ReconciliationStatus Status { get; set; }
    public decimal TotalDifferenceTRY { get; set; }

    public List<ReconciliationItem> Items { get; set; } = new();
}

public class ReconciliationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReconciliationId { get; set; }
    public int Slot { get; set; }
    public int SystemCount { get; set; }
    public int PhysicalCount { get; set; }
    public int Difference { get; set; }
}

public record ReconciliationCount(int Slot, int Count);

public record ReconciliationRequest(
    List<ReconciliationCount> Counts
);


public enum MovementType
{
    LOAD,
    UNLOAD,
    WITHDRAWAL,
    DEPOSIT,
    RESERVATION,
    RESERVATION_RELEASE,
    RECONCILIATION_ADJUSTMENT
}

public class CassetteMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CassetteId { get; set; }

    public Currency Currency { get; set; }

    public int Denomination { get; set; }

    public int QuantityChange { get; set; }

    public MovementType Type { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public Guid? ReferenceId { get; set; }
}
