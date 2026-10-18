# Update 55 – Development users data source for select

- Development DI registers `DummyJsonataCaller : IFormDataBackend`; production retains `GraphqlLikeFormDataBackend`.
- Dummy uses `IUserService.GetAllAsync` (the existing secured UserResponse service), **no new HTTP endpoint** or duplicate fake-user fixture.
- All three phases remain public: `PreprocessAsync`, `FetchAsync`, `PostprocessAsync`; `ExecuteAsync` composes the phases.
- Select renderer accepts `configuration_json: {"dataSource":"users"}` and obtains dynamic `{label,value}` items. Existing static `options` continue to work.
- User postprocessing preserves all original fields; adds `value=id`, `label=Name + surname`.
- Run with `ASPNETCORE_ENVIRONMENT=Development`; designer/test view uses the same renderer.
- This build environment does not contain the .NET SDK; compile and integration tests must be run in a .NET 8 environment.
