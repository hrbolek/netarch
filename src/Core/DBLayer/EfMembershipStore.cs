using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public sealed class EfMembershipStore(ApplicationDbContext db) : EfStore<MembershipModel>(db), IMembershipStore;
