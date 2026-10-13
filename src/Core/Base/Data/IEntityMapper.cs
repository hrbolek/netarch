namespace WebAppArchitecture.Base.Data;

/// <summary>Map an already materialized database entity to an output DTO.</summary>
public interface IEntityMapper<TDBModel, TDTO> : IEntityDTOModel
    where TDBModel : IEntityDBModel
    where TDTO : IEntityMapper<TDBModel, TDTO>
{
    static abstract TDTO FromModel(TDBModel model);
}
