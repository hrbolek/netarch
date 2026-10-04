# Architecture Update 16.6

## EF Core generic GetById query

`BaseService.GetByIdAsync` now applies the identifier predicate to the secured database entity query before DTO projection.

Pipeline:

```text
Security.Query<TDBModel>()
  -> Where(entity.id == id)
  -> Select(TDTOModel.Projection)
  -> SingleOrDefault
```

This avoids an EF Core translation failure caused by filtering the projected DTO through the non-generic `IEntityDTOModel` interface.
