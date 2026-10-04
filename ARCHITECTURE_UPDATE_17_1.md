# Architecture Update 17.1

Compile fix after the authorization/list refactor.

`EntityRelationsViewModel<TParent,TChild>.Items` now uses `IReadOnlyCollection<TChild>` instead of `TChild[]`.

This matches the `BaseService.VectorAsync()` return contract and avoids needless array conversions in Group/User relation resolvers.
