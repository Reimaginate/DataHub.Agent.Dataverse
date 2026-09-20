using System.Reflection;
using System.Runtime.ExceptionServices;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub;
using Xunit;
using Account = DataverseModel.Account;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.TechnicalRegressions;

// Risk: removing parallel scheduling can accidentally drop cancellation checks.
// Consequence: cancelled outbound work continues calculating updates. Earlier async
// handler boundaries cannot deterministically place cancellation inside this loop.
public sealed class OutboundComparisonCancellationTests
{
    [Theory(DisplayName = "Cancelled outbound comparison stops before inspecting attributes")]
    [Trait("Category", "Unit")]
    [Trait("ScenarioKind", "TechnicalRegression")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AlreadyCancelledComparisonStops(bool empty)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var original = new Account();
        var changed = new Account();
        if (!empty) { original.Name = "Original"; changed.Name = "Changed"; }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compare(original, changed, cancellation.Token));
    }

    [Fact(DisplayName = "Cancellation during outbound comparison prevents inspecting the next field")]
    [Trait("Category", "Unit")]
    [Trait("ScenarioKind", "TechnicalRegression")]
    public async Task CancellationBetweenFieldsStopsComparison()
    {
        using var cancellation = new CancellationTokenSource();
        var original = new Account { ["first"] = new CancelOnComparison(cancellation), ["second"] = new MustNotCompare() };
        var changed = new Account { ["first"] = new object(), ["second"] = new object() };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Compare(original, changed, cancellation.Token));
    }

    private static async Task Compare(Account original, Account changed, CancellationToken cancellation)
    {
        var handler = new ProcessUpdatedEntitiesRequestHandler<CalendarAccount, Account>(null, null, null, null, null);
        var method = handler.GetType().GetMethod("AreDataverseEntitiesEqual", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            await (Task)method.Invoke(handler, [original, changed, new List<string> { "name", "first", "second" }, new List<string>(), cancellation])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private sealed class CancelOnComparison(CancellationTokenSource cancellation)
    {
        public override bool Equals(object obj) { cancellation.Cancel(); return true; }
        public override int GetHashCode() => 0;
    }

    private sealed class MustNotCompare
    {
        public override bool Equals(object obj) => throw new InvalidOperationException("Comparison continued after cancellation.");
        public override int GetHashCode() => 0;
    }
}
