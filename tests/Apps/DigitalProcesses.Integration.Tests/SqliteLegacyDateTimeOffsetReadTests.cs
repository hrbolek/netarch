using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class SqliteLegacyDateTimeOffsetReadTests
{
    [Theory]
    [InlineData("2026-10-10 11:23:25.0792985+00:00")]
    [InlineData("2026-10-10T11:23:25.0792985+00:00")]
    public void DP29_Sqlite_ConverterCanReadLegacyAndCanonicalUtc(string stored)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new ApplicationDbContext(options);
        var entity = db.Model.FindEntityType(typeof(RbacAccessBindingModel))!;
        var property = entity.FindProperty(nameof(RbacAccessBindingModel.valid_to))!;
        var converter = property.GetValueConverter()!;

        var value = Assert.IsType<DateTimeOffset>(converter.ConvertFromProvider(stored));
        Assert.Equal(new DateTimeOffset(2026, 10, 10, 11, 23, 25, TimeSpan.Zero).AddTicks(792985), value);
        var serialized = Assert.IsType<string>(converter.ConvertToProvider(value));
        Assert.StartsWith("2026-10-10T11:23:25.0792985", serialized);
    }
}
