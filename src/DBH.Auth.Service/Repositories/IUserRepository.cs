
using DBH.Auth.Service.Models.Entities;

namespace DBH.Auth.Service.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailWithRolesAsync(string email);
    Task<User?> GetByIdWithProfileAsync(Guid userId);
    Task<List<User>> GetDoctorsByOrganizationAsync(string organizationId);
    Task<User?> GetDoctorByUserIdAndOrganizationAsync(Guid userId, string organizationId);
}
