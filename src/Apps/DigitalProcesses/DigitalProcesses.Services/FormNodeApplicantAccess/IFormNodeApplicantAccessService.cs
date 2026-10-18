using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;



public interface IFormNodeApplicantAccessService : ISecuredEntityService<FormNodeApplicantAccessModel>, IApplicantPolicyService { }
