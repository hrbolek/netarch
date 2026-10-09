using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

public interface IGroupTypeService : IBaseService<GroupTypeModel, GroupTypeResponse> { }
