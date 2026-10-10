namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Pure target-selection policy. This answers WHERE a published process may be started,
/// not WHO is allowed to start it. StartAsync must separately demand Create authorization.
/// This policy is intentionally independent of a persistence provider.
/// </summary>
public static class ProcessTargetGroupPolicy
{
    public static bool IsAllowed(
        Guid targetGroupId,
        Guid targetGroupTypeId,
        IReadOnlyCollection<ProcessTargetGroupRule> rules,
        bool targetGroupValid = true)
    {
        if (!targetGroupValid || targetGroupId == Guid.Empty || targetGroupTypeId == Guid.Empty || rules.Count == 0)
            return false; // explicit fail-closed configuration
        return rules.Any(r =>
            (r.TargetGroupId is Guid id && id == targetGroupId) ||
            (r.TargetGroupTypeId is Guid type && type == targetGroupTypeId));
    }
}

/// <summary>A process-version target allow rule; exactly one selector must be supplied.</summary>
public sealed record ProcessTargetGroupRule(Guid? TargetGroupId, Guid? TargetGroupTypeId)
{
    public bool IsValid => (TargetGroupId.HasValue ^ TargetGroupTypeId.HasValue)
        && (TargetGroupId ?? TargetGroupTypeId) != Guid.Empty;
}
