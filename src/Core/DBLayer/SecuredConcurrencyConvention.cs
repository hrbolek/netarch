using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

internal sealed class SecuredConcurrencyConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entity in modelBuilder.Metadata.GetEntityTypes()
            .Where(entity => typeof(ISecuredEntityDBModel).IsAssignableFrom(entity.ClrType)))
            entity.FindProperty(nameof(ISecuredEntityDBModel.updated_at))?.Builder.IsConcurrencyToken(true);
    }
}
