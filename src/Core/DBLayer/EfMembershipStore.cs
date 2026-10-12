using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef Membership Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public sealed class EfMembershipStore(ApplicationDbContext db) : EfStore<MembershipModel>(db), IMembershipStore;
