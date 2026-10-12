using Atm.Api;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AtmDb>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Atm")

    ?? "Host=localhost;Database=atm;Username=atm;Password=atm"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks().AddDbContextCheck<AtmDb>();

var app = builder.Build();

app.UseSwagger(); app.UseSwaggerUI();

app.MapHealthChecks("/health");



// Apply pending EF Core migrations on startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AtmDb>();
    for (var i = 0; i < 15; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Npgsql.NpgsqlException) when (i < 14)
        {
            await Task.Delay(1000);
        }
    }
}

static IResult Error(string code, string message, int status = 400) =>

    Results.Problem(statusCode: status, title: code, detail: message, extensions: new Dictionary<string, object?> { ["errorCode"] = code });

static bool ValidDenom(int x) => x is 20 or 50 or 100 or 500;

static async Task<AtmMachine?> FindAtm(AtmDb db, Guid id) => await db.Atms.Include(a => a.Cassettes).ThenInclude(c => c.DepositNotes).FirstOrDefaultAsync(a => a.Id == id);



app.MapPost("/api/atms", async (CreateAtm req, AtmDb db) => {

    if (string.IsNullOrWhiteSpace(req.Name)) return Error("INVALID_NAME", "Name is required.");

    var atm = new AtmMachine { Name = req.Name.Trim() };

    db.Atms.Add(atm); await db.SaveChangesAsync();

    return Results.Created($"/api/atms/{atm.Id}", new { atm.Id, atm.Name, atm.State });

});

app.MapGet("/api/atms", async (AtmDb db) => await db.Atms.Select(a => new { a.Id, a.Name, a.State }).ToListAsync());

app.MapGet("/api/atms/{id:guid}", async (Guid id, AtmDb db) => {

    var atm = await FindAtm(db, id);

    return atm is null ? Error("ATM_NOT_FOUND", "ATM does not exist.", 404) : Results.Ok(atm);

});

app.MapPost("/api/atms/{id:guid}/configure", async (Guid id, List<CassetteConfiguration> input, AtmDb db) => {

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    if (atm.State is not (AtmState.UNCONFIGURED or AtmState.CONFIGURED or AtmState.MAINTENANCE)) return Error("WRONG_STATE", "Configure only during setup or maintenance.", 409);

    if (input.Count != 5 || input.Select(c => c.Slot).Distinct().Count() != 5 || input.Any(c => c.Slot < 1 || c.Slot > 5)

        || input.Count(c => c.Kind == CassetteKind.Deposit) != 1 || input.Count(c => c.Kind == CassetteKind.Recycling) != 4)

        return Error("INVALID_CONFIGURATION", "Exactly five numbered cassettes: one deposit and four recycling.");

    if (input.Any(c => c.Kind == CassetteKind.Recycling && (c.Currency is null || c.Denomination is null || !ValidDenom(c.Denomination.Value)))

        || input.Any(c => c.Kind == CassetteKind.Deposit && (c.Currency is not null || c.Denomination is not null)))

        return Error("INVALID_CONFIGURATION", "Deposit cassette must be untyped; recycling cassettes need valid currency and denomination.");

    var foreign = input.Count(c => c.Kind == CassetteKind.Recycling && c.Currency is (Currency.USD or Currency.EUR));

    if (foreign < 1 || foreign > 2) return Error("FOREIGN_CASSETTE_RULE", "One or two recycling cassettes must be foreign currency.");

    if (atm.Cassettes.Any(c => c.Count != 0)) return Error("CASH_PRESENT", "Unload cash before reconfiguration.", 409);

    // Register each replacement explicitly as Added. Replacing the tracked navigation

    // collection alone can cause EF Core to issue UPDATE for rows not yet in the DB.

    db.Cassettes.RemoveRange(atm.Cassettes);

    var newCassettes = input.Select(c => new Cassette {

        AtmMachineId = atm.Id,

        Slot = c.Slot,

        Kind = c.Kind,

        Currency = c.Currency,

        Denomination = c.Denomination

    }).ToList();

    db.Cassettes.AddRange(newCassettes);

    if (atm.State != AtmState.MAINTENANCE) atm.State = AtmState.CONFIGURED;

    await db.SaveChangesAsync(); return Results.Ok(new { atm.Id, atm.State });

});

app.MapPost("/api/atms/{id:guid}/load", async (Guid id, List<CashLoad> loads, AtmDb db) => {

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    if (atm.State is not (AtmState.CONFIGURED or AtmState.MAINTENANCE)) return Error("WRONG_STATE", "Load only after configuration or during maintenance.", 409);

    if (loads.Count == 0 || loads.Any(x => x.Count < 0 || !atm.Cassettes.Any(c => c.Slot == x.Slot)) || loads.Select(x => x.Slot).Distinct().Count() != loads.Count)

        return Error("INVALID_LOAD", "Invalid cassette slot or quantity; duplicate slots are not allowed.");

    foreach (var x in loads) {

        var cassette = atm.Cassettes.Single(c => c.Slot == x.Slot);

        if (cassette.Kind == CassetteKind.Deposit && x.Count != 0) return Error("INVALID_LOAD", "Use deposit endpoint to add notes to the mixed-currency cassette.");

        if (cassette.Count + x.Count > 100) return Error("CAPACITY_EXCEEDED", "Cassette capacity is 100 notes.");

    }

    foreach (var x in loads) atm.Cassettes.Single(c => c.Slot == x.Slot).Count += x.Count;

    if (atm.State == AtmState.CONFIGURED) atm.State = AtmState.LOADED;

    await db.SaveChangesAsync(); return Results.Ok(new { atm.Id, atm.State });

});

app.MapPost("/api/atms/{id:guid}/state", async (Guid id, ChangeState input, AtmDb db) => {

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    var allowed = (atm.State == AtmState.LOADED && input.State == AtmState.IN_SERVICE)

        || (atm.State == AtmState.IN_SERVICE && input.State == AtmState.MAINTENANCE)

        || (atm.State == AtmState.MAINTENANCE && input.State == AtmState.IN_SERVICE);

    if (!allowed) return Error("INVALID_TRANSITION", $"Cannot move from {atm.State} to {input.State}.", 409);

    atm.State = input.State; await db.SaveChangesAsync(); return Results.Ok(new { atm.Id, atm.State });

});

app.MapPost("/api/atms/{id:guid}/withdrawal-options", async (Guid id, WithdrawalRequest req, AtmDb db) => {

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    if (atm.State != AtmState.IN_SERVICE) return Error("WRONG_STATE", "ATM is not in service.", 409);

    if (req.Amount <= 0 || req.DailyRemainingLimit is < 0) return Error("INVALID_AMOUNT", "Amount must be positive and limit non-negative.");

    var stock = atm.Cassettes.Where(c => c.Kind == CassetteKind.Recycling && c.Currency == req.Currency)

        .Select(c => new NoteAmount(c.Denomination!.Value, c.Count));

    return Results.Ok(NoteSolver.Solve(stock, req.Amount, req.DailyRemainingLimit));

});

app.MapPost("/api/atms/{id:guid}/withdraw", async (Guid id, ConfirmWithdrawal req, AtmDb db) => {

    // This is a simple one-step endpoint for milestone 1, NOT the B4 quote/reservation workflow.

    await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    if (atm.State != AtmState.IN_SERVICE) return Error("WRONG_STATE", "ATM is not in service.", 409);

    if (req.Amount <= 0) return Error("INVALID_AMOUNT", "Amount must be positive.");

    var cassettes = atm.Cassettes.Where(c => c.Kind == CassetteKind.Recycling && c.Currency == req.Currency).ToList();

    var options = NoteSolver.Solve(cassettes.Select(c => new NoteAmount(c.Denomination!.Value, c.Count)), req.Amount);

    if (options.Exact is null) return Error("NO_EXACT_MATCH", "Requested amount is unavailable; inspect withdrawal-options first.", 409);

    foreach (var note in options.Exact.Notes) {

        var remaining = note.Count;

        foreach (var cassette in cassettes.Where(c => c.Denomination == note.Denomination)

                     .OrderByDescending(c => c.Count / 100.0).ThenBy(c => c.Slot)) {

            var taking = Math.Min(remaining, cassette.Count); cassette.Count -= taking; remaining -= taking;

            if (remaining == 0) break;

        }

    }

    await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(options.Exact);

});

app.MapPost("/api/atms/{id:guid}/deposit", async (Guid id, DepositRequest req, AtmDb db) => {

    await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    if (atm.State != AtmState.IN_SERVICE) return Error("WRONG_STATE", "ATM is not in service.", 409);

    if (req.UnrecognisedCount < 0 || req.Notes is null || req.Notes.Any(n => n.Count < 0 || !ValidDenom(n.Denomination))) return Error("INVALID_DEPOSIT", "Invalid denomination or count.");

    var deposit = atm.Cassettes.Single(c => c.Kind == CassetteKind.Deposit);

    var allocated = new List<DepositAllocation>(); var returned = new List<NoteInput>();

    foreach (var n in req.Notes) {

        var remaining = n.Count;

        foreach (var c in atm.Cassettes.Where(c => c.Kind == CassetteKind.Recycling && c.Currency == n.Currency && c.Denomination == n.Denomination).OrderBy(c => c.Slot)) {

            var quantity = Math.Min(remaining, 100 - c.Count);

            if (quantity > 0) { c.Count += quantity; remaining -= quantity; allocated.Add(new(c.Slot, n.Currency, n.Denomination, quantity)); }

        }

        var accepted = Math.Min(remaining, 100 - deposit.Count);

        if (accepted > 0) {

            deposit.Count += accepted; remaining -= accepted;

            var item = deposit.DepositNotes.SingleOrDefault(x => x.Currency == n.Currency && x.Denomination == n.Denomination);

            if (item is null) deposit.DepositNotes.Add(new DepositNote { Currency = n.Currency, Denomination = n.Denomination, Count = accepted });

            else item.Count += accepted;

            allocated.Add(new(deposit.Slot, n.Currency, n.Denomination, accepted));

        }

        if (remaining > 0) returned.Add(new(n.Currency, n.Denomination, remaining));

    }

    await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(new { allocated, returned, req.UnrecognisedCount });

});

app.MapGet("/api/atms/{id:guid}/report", async (Guid id, AtmDb db) => {

    var atm = await FindAtm(db, id);

    if (atm is null) return Error("ATM_NOT_FOUND", "ATM does not exist.", 404);

    var notes = atm.Cassettes.SelectMany(c => c.Kind == CassetteKind.Deposit

        ? c.DepositNotes.Select(n => new { c.Slot, n.Currency, n.Denomination, n.Count })

        : new[] { new { c.Slot, Currency = c.Currency!.Value, Denomination = c.Denomination!.Value, c.Count } }).ToList();

    return Results.Ok(new { atm.Id, atm.Name, atm.State, cassettes = notes,

        totalByCurrency = notes.GroupBy(x => x.Currency).Select(g => new { Currency = g.Key, Total = g.Sum(n => n.Denomination * n.Count) }) });

});



app.MapPost("/api/atms/{id:guid}/reconcile",
    async (
        Guid id,
        ReconciliationRequest request,
        AtmDb db,
        HttpContext context) =>
{
    var atm = await db.Atms
        .Include(a => a.Cassettes)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (atm is null)
        return Error("ATM_NOT_FOUND",
            "ATM does not exist.", 404);

    if (atm.State != AtmState.MAINTENANCE)
        return Error("WRONG_STATE",
            "Reconciliation requires maintenance mode.", 409);

    if (request.Counts is null ||
        request.Counts.Count != atm.Cassettes.Count ||
        request.Counts.Select(x => x.Slot).Distinct().Count()
            != atm.Cassettes.Count)
        return Error("INVALID_COUNTS",
            "Provide one count for each cassette.");

    if (request.Counts.Any(x =>
        x.Count < 0 || x.Count > 100 ||
        !atm.Cassettes.Any(c => c.Slot == x.Slot)))
        return Error("INVALID_COUNTS",
            "Invalid cassette slot or count.");

    var user = context.Request.Headers["X-User"]
        .ToString();

    if (string.IsNullOrWhiteSpace(user))
        return Error("MISSING_USER",
            "X-User header is required.");

    var reconciliation = new Reconciliation
    {
        AtmMachineId = atm.Id,
        CreatedBy = user,
        Status = ReconciliationStatus.Completed
    };

    foreach (var input in request.Counts)
    {
        var cassette = atm.Cassettes
            .Single(c => c.Slot == input.Slot);

        reconciliation.Items.Add(
            new ReconciliationItem
            {
                Slot = input.Slot,
                SystemCount = cassette.Count,
                PhysicalCount = input.Count,
                Difference = input.Count - cassette.Count
            });
    }

    db.Reconciliations.Add(reconciliation);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        reconciliation.Id,
        reconciliation.Status,
        reconciliation.Items
    });
});

app.Run();

public record CreateAtm(string Name);

public record ChangeState(AtmState State);

public record ConfirmWithdrawal(Currency Currency, int Amount);

public partial class Program { }


