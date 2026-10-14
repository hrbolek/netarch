using System.Globalization;

namespace WebAppArchitecture.DigitalProcesses.UI.Designer;

public sealed record WorkflowEdgeGeometry(string Path, double LabelX, double LabelY)
{
    public static double Bend(int sourceX, int targetX, int slot)
        => (45 + Math.Abs(targetX - sourceX) / 4 + slot * 28) / 3d;

    public static WorkflowEdgeGeometry Create(int sourceX, int targetX, int nodeY, int slot)
    {
        var backwards = targetX <= sourceX;
        var startY = nodeY + 40d;
        var controlY = startY + Bend(sourceX, targetX, slot) * 4d / 3;
        var labelY = startY + Bend(sourceX, targetX, slot) + 14;
        // Reflect the same lower curve about the node centre for reverse transitions.
        double Y(double y) => backwards ? 2 * (nodeY + 20d) - y : y;
        var offset = (targetX - sourceX) / 5d;
        var startX = (double)sourceX;
        var endX = (double)targetX;
        var controlX1 = sourceX + offset;
        var controlX2 = targetX - offset;
        if (sourceX == targetX)
        {
            startX -= 24;
            endX += 24;
            controlX1 = sourceX - 100;
            controlX2 = sourceX + 100;
        }
        var path = string.Create(CultureInfo.InvariantCulture,
            $"M {startX:0.###} {Y(startY):0.###} C {controlX1:0.###} {Y(controlY):0.###}, {controlX2:0.###} {Y(controlY):0.###}, {endX:0.###} {Y(startY):0.###}");
        return new WorkflowEdgeGeometry(path, (sourceX + targetX) / 2d, Y(labelY));
    }
}
