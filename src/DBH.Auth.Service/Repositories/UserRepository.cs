
using DBH.Auth.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;
using DBH.Auth.Service.Data;

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
}
