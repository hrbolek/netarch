using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class SqliteDateTimeOffsetConversionTests
{
    [Fact]
    public void DP27_Sqlite_ConvertsAllOffsetPropertiesToUtcText()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new ApplicationDbContext(options);

        var properties = db.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(DateTimeOffset) ||
                               property.ClrType == typeof(DateTimeOffset?))
            .ToArray();

        Assert.NotEmpty(properties);
        Assert.All(properties, property => Assert.NotNull(property.GetValueConverter()));

        var validTo = db.Model.FindEntityType(typeof(RbacAccessBindingModel))!
            .FindProperty(nameof(RbacAccessBindingModel.valid_to))!;
        var converter = validTo.GetValueConverter()!;
        var utc = new DateTimeOffset(2026, 10, 10, 15, 30, 0, TimeSpan.Zero);
        var otherOffset = utc.ToOffset(TimeSpan.FromHours(2));

        Assert.Equal(converter.ConvertToProvider(utc), converter.ConvertToProvider(otherOffset));
        Assert.EndsWith("+00:00", Assert.IsType<string>(converter.ConvertToProvider(otherOffset)));
    }

    [Fact]
    public void DP28_Sqlite_TranslatesDateTimeOffsetIntervalFilterToSql()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new ApplicationDbContext(options);
        var now = DateTimeOffset.UtcNow;
        var scope = Guid.NewGuid();
        var target = Guid.NewGuid();

        var query = db.RbacAccessBindings.Where(x =>
            x.managed_scope_id == scope &&
            x.target_rbacobject_id == target &&
            (x.valid_to == null || x.valid_to > now));

        // Translation used to throw InvalidOperationException on SQLite.
        var sql = query.ToQueryString();
        Assert.Contains("valid_to", sql);
        Assert.Contains("rbac_access_bindings", sql);
    }
}
