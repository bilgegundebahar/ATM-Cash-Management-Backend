using Atm.Api;
using Xunit;
namespace Atm.Tests;
public class NoteSolverTests {
    static NoteAmount N(int denomination, int count) => new(denomination, count);
    static void Check(WithdrawalChoice? result, int expected, params NoteAmount[] breakdown) {
        Assert.NotNull(result); Assert.Equal(expected, result.Amount);
        Assert.Equal(breakdown.OrderBy(n => n.Denomination), result.Notes.OrderBy(n => n.Denomination));
    }
    [Fact] public void Case1() { var r = NoteSolver.Solve(new[] { N(100,0), N(50,3), N(20,10) },160); Check(r.Exact,160,N(50,2),N(20,3)); }
    [Fact] public void Case2() { var r = NoteSolver.Solve(new[] { N(50,1),N(20,2) },80); Assert.Null(r.Exact); Check(r.Lower,70,N(50,1),N(20,1)); Check(r.Upper,90,N(50,1),N(20,2)); }
    [Fact] public void Case3() { var r = NoteSolver.Solve(new[] { N(100,5),N(20,10) },130); Assert.Null(r.Exact); Check(r.Lower,120,N(100,1),N(20,1)); Check(r.Upper,140,N(100,1),N(20,2)); }
    [Fact] public void Case4() { var r = NoteSolver.Solve(new[] { N(100,2) },250); Assert.Null(r.Exact); Check(r.Lower,200,N(100,2)); Assert.Null(r.Upper); }
    [Fact] public void Case5() { var r = NoteSolver.Solve(new[] { N(500,1),N(100,0),N(20,5) },600); Check(r.Exact,600,N(500,1),N(20,5)); }
    [Fact] public void Case6() { var r = NoteSolver.Solve(new[] { N(100,1),N(50,4),N(20,5) },200); Check(r.Exact,200,N(100,1),N(50,2)); }
    [Fact] public void Case7() { var r = NoteSolver.Solve(new[] { N(500,1),N(100,3),N(20,20) },1000); Check(r.Exact,1000,N(500,1),N(100,3),N(20,10)); }
    [Fact] public void Case8() { var r = NoteSolver.Solve(new[] { N(100,3),N(50,1),N(20,1) },110); Assert.Null(r.Exact); Check(r.Lower,100,N(100,1)); Check(r.Upper,120,N(100,1),N(20,1)); }
    [Fact] public void CaseUpperLimit() { var r = NoteSolver.Solve(new[] { N(50,1),N(20,2) },80,80); Assert.Null(r.Upper); }
}
