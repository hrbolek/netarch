using WebAppArchitecture.DigitalProcesses.UI.WorkflowVersion.Components;
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class WorkflowEdgeGeometryTests
{
    [Fact]
    public void ReverseCurve_IsExactVerticalReflectionWithReversedEndpoints()
    {
        const int nodeY = 220;
        var forward = WorkflowEdgeGeometry.Create(100, 460, nodeY, 0);
        var backward = WorkflowEdgeGeometry.Create(460, 100, nodeY, 0);
        var first = Coordinates(forward.Path);
        var second = Coordinates(backward.Path);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(first[i * 2], second[(3 - i) * 2]);
            Assert.Equal(2 * (nodeY + 20d), first[i * 2 + 1] + second[(3 - i) * 2 + 1], 3);
        }
        Assert.Equal(forward.LabelX, backward.LabelX);
        Assert.Equal(2 * (nodeY + 20d), forward.LabelY + backward.LabelY);
    }

    [Fact]
    public void TwoReturnsToFirstState_HaveDifferentHeightsBasedOnDistance()
    {
        var shorter = WorkflowEdgeGeometry.Create(280, 100, 220, 0);
        var longer = WorkflowEdgeGeometry.Create(460, 100, 220, 0);
        Assert.Equal(30, WorkflowEdgeGeometry.Bend(280, 100, 0));
        Assert.Equal(45, WorkflowEdgeGeometry.Bend(460, 100, 0));
        Assert.Equal(15, WorkflowEdgeGeometry.Bend(460, 100, 0) - WorkflowEdgeGeometry.Bend(280, 100, 0));
        Assert.True(longer.LabelY < shorter.LabelY);
        Assert.True(Coordinates(longer.Path)[3] < Coordinates(shorter.Path)[3]);
    }

    [Fact]
    public void ParallelEdgesAndSelfLoops_AreDistinct_AndUseInvariantSvgNumbers()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
            var first = WorkflowEdgeGeometry.Create(100, 280, 200, 0);
            var second = WorkflowEdgeGeometry.Create(100, 280, 200, 1);
            Assert.True(second.LabelY > first.LabelY);
            Assert.Matches(@"\d+\.\d+", second.Path);
            var loop = Coordinates(WorkflowEdgeGeometry.Create(100, 100, 200, 0).Path);
            Assert.NotEqual(loop[0], loop[6]);
            Assert.True(loop[2] < loop[0]);
            Assert.True(loop[4] > loop[6]);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static double[] Coordinates(string path) => Regex.Matches(path, @"-?\d+(?:\.\d+)?")
        .Select(x => double.Parse(x.Value, CultureInfo.InvariantCulture)).ToArray();
}
