using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta I Role Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public interface IRoleStore : IBaseStore<RoleModel> { }
