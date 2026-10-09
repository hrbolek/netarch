# ZArchitecture56 – GraphQL configuration and section variables

For a `select` node, use the target data source contract:

```json
{
  "dataSource": {
    "query": "query FindUsers($department: String) { users(department: $department) { id name surname } }",
    "variables": "section"
  }
}
```

`query` is an arbitrary GraphQL document. The development `DummyJsonataCaller` intentionally ignores it and retrieves users from the same secured `IUserService` as `/architecture/UserResponse/list`. The existing PreprocessAsync / FetchAsync / PostprocessAsync / ExecuteAsync hooks remain intact. The postprocess step adds `value` and `label` for select options.

`variables: section` identifies the section containing the select as variable scope. Each sibling field's `key` becomes a variable name; the currently available value is passed through `FormValueSnapshot`. Missing values are null during initial server-side rendering. The current designer/test markup is non-interactive server-rendered HTML: it does not yet synchronize user-edited browser values with server-side snapshots. That later interaction is intentionally not represented as working functionality.

Static `options` are still supported. No new user endpoint was added.
