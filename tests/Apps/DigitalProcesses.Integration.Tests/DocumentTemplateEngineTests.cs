using System.Text;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class DocumentTemplateEngineTests
{
    [Fact]
    public void DP30_RtfRenderer_EscapesInsertedValues()
    {
        var source = Encoding.Latin1.GetBytes(@"{\rtf1 [[I:decision]]}");
        Assert.True(RtfTemplateEngine.IsValidTemplate(source, out _));
        var data = new JsonObject { ["decision"] = "Approved {conditionally} \\ yes" };
        var rendered = Encoding.Latin1.GetString(RtfTemplateEngine.Render(source, data, Guid.NewGuid()));
        Assert.Contains(@"Approved \{conditionally\} \\ yes", rendered);
    }

    [Fact]
    public void DP31_RtfRenderer_RejectsUnbalancedBlocks()
    {
        var source = Encoding.Latin1.GetBytes(@"{\rtf1 [[W:applicant]]}");
        Assert.False(RtfTemplateEngine.IsValidTemplate(source, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
