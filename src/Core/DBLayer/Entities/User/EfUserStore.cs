using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef User Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public sealed class EfUserStore(CoreDbContext db) : EfStore<UserModel>(db), IUserStore;
