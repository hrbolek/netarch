using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

/// <summary>
/// DP19–DP23: target-selection policy contract for future ProcessVersion -> target group rules.
/// These are pure policy tests. They DO NOT assert that HTTP Start accepts a target group:
/// its current DTO lacks TargetRbacObjectId and runtime integration is pending.
/// </summary>
public sealed class DigitalProcessesTargetGroupPolicyTests
{
    private static readonly Guid FacultyType = Guid.NewGuid();
    private static readonly Guid OtherType = Guid.NewGuid();
    private static readonly Guid FacultyA = Guid.NewGuid();
    private static readonly Guid FacultyB = Guid.NewGuid();

    [Fact]
    public void DP19_GroupType_EnablesSeveralFacultiesWithOneRule()
    {
        var rules = new[] { new ProcessTargetGroupRule(null, FacultyType) };
        Assert.True(ProcessTargetGroupPolicy.IsAllowed(FacultyA, FacultyType, rules));
        Assert.True(ProcessTargetGroupPolicy.IsAllowed(FacultyB, FacultyType, rules));
    }

    [Fact]
    public void DP20_ExplicitGroupRule_RestrictsEvenGroupsOfSameType()
    {
        var rules = new[] { new ProcessTargetGroupRule(FacultyA, null) };
        Assert.True(ProcessTargetGroupPolicy.IsAllowed(FacultyA, FacultyType, rules));
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(FacultyB, FacultyType, rules));
    }

    [Fact]
    public void DP21_WrongGroupType_IsNotAnAllowedTarget()
    {
        var rules = new[] { new ProcessTargetGroupRule(null, FacultyType) };
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(Guid.NewGuid(), OtherType, rules));
    }

    [Fact]
    public void DP22_NoRules_OrMissingTarget_FailsClosed()
    {
        var rules = new[] { new ProcessTargetGroupRule(null, FacultyType) };
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(FacultyA, FacultyType, Array.Empty<ProcessTargetGroupRule>()));
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(Guid.Empty, FacultyType, rules));
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(FacultyA, Guid.Empty, rules));
    }

    [Fact]
    public void DP24_RetiredGroup_IsRejected_EvenWhenTypeAndIdMatch()
    {
        var rules = new[] { new ProcessTargetGroupRule(null, FacultyType) };
        Assert.True(ProcessTargetGroupPolicy.IsAllowed(FacultyA, FacultyType, rules, targetGroupValid: true));
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(FacultyA, FacultyType, rules, targetGroupValid: false));
        Assert.False(ProcessTargetGroupPolicy.IsAllowed(FacultyA, FacultyType,
            new[] { new ProcessTargetGroupRule(FacultyA, null) }, targetGroupValid: false));
    }

    [Fact]
    public void DP23_RulesMustSelectExactlyOneDimension()
    {
        Assert.True(new ProcessTargetGroupRule(FacultyA, null).IsValid);
        Assert.True(new ProcessTargetGroupRule(null, FacultyType).IsValid);
        Assert.False(new ProcessTargetGroupRule(null, null).IsValid);
        Assert.False(new ProcessTargetGroupRule(FacultyA, FacultyType).IsValid);
        Assert.False(new ProcessTargetGroupRule(null, Guid.Empty).IsValid);
    }
}
