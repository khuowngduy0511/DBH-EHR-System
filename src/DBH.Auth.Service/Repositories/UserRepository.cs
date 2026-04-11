
using DBH.Auth.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;
using DBH.Auth.Service.DbContext;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AuthDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailWithRolesAsync(string email)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdWithProfileAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.DoctorProfile)
            .Include(u => u.PatientProfile)
            .Include(u => u.StaffProfile)  // Gộp Nurse, Pharmacist, LabTech, Receptionist
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByEmailWithProfileAsync(string email)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.DoctorProfile)
            .Include(u => u.PatientProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByPhoneWithProfileAsync(string phone)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.DoctorProfile)
            .Include(u => u.PatientProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    public async Task<List<User>> GetDoctorsByOrganizationAsync(string organizationId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.OrganizationId == organizationId)
            .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == RoleName.Doctor))
            .ToListAsync();
    }

    public async Task<User?> GetDoctorByUserIdAndOrganizationAsync(Guid userId, string organizationId)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                u.UserId == userId &&
                u.OrganizationId == organizationId &&
                u.UserRoles.Any(ur => ur.Role.RoleName == RoleName.Doctor));
    }
}
