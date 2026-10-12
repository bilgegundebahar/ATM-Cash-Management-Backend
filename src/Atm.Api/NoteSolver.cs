namespace Atm.Api;
/// <summary>Bounded dynamic programming: minimum note count for every achievable sum.</summary>
public static class NoteSolver {
    public static WithdrawalOptions Solve(IEnumerable<NoteAmount> stock, int requested, int? dailyRemainingLimit = null) {
        if (requested <= 0) throw new ArgumentException("Requested amount must be positive.");
        if (dailyRemainingLimit is < 0) throw new ArgumentException("Limit must not be negative.");
        var notes = stock.Where(n => n.Count > 0).GroupBy(n => n.Denomination)
            .Select(g => new NoteAmount(g.Key, g.Sum(n => n.Count))).OrderByDescending(n => n.Denomination).ToArray();
        if (notes.Any(n => n.Denomination is not (20 or 50 or 100 or 500) || n.Count < 0))
            throw new ArgumentException("Unsupported denomination.");
        var reachable = new Dictionary<int, int[]> { [0] = new int[notes.Length] };
        for (var i = 0; i < notes.Length; i++) {
            var next = new Dictionary<int, int[]>();
            foreach (var (amount, counts) in reachable) {
                for (var take = 0; take <= notes[i].Count; take++) {
                    var sum = amount + take * notes[i].Denomination;
                    var proposed = (int[])counts.Clone(); proposed[i] = take;
                    if (!next.TryGetValue(sum, out var old) || proposed.Sum() < old.Sum())
                        next[sum] = proposed;
                }
            }
            reachable = next;
        }
        WithdrawalChoice ToChoice(int amount) => new(amount, notes.Select((n, i) => new NoteAmount(n.Denomination, reachable[amount][i])).Where(n => n.Count > 0).ToList());
        var limit = dailyRemainingLimit ?? int.MaxValue;
        WithdrawalChoice? exact = reachable.ContainsKey(requested) && requested <= limit ? ToChoice(requested) : null;
        var lower = reachable.Keys.Where(x => x > 0 && x < requested && x <= limit).DefaultIfEmpty(-1).Max();
        var upper = reachable.Keys.Where(x => x > requested && x <= limit).DefaultIfEmpty(int.MaxValue).Min();
        return new(exact, exact is null && lower > 0 ? ToChoice(lower) : null,
            exact is null && upper != int.MaxValue ? ToChoice(upper) : null);
    }
}
