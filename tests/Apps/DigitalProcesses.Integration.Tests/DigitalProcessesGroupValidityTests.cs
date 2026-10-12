using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DBLayer;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class DigitalProcessesGroupValidityTests
{
    [Fact]
    public void DP25_Group_ActiveByDefault_AndCanBeRetiredWithoutChangingIdentity()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var group = new GroupModel(id, "Faculty", Guid.NewGuid(), null,
            $"/{id:D}/", true, id, now, now, actor, actor);
        Assert.True(group.valid);
        var retired = group with { valid = false };
        Assert.False(retired.valid);
        Assert.Equal(group.id, retired.id);
        Assert.Equal(group.path, retired.path);
    }
}
