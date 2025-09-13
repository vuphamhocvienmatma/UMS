using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UMS.Dtos;
using UMS.Models.Entities;
using UMS.Services;

namespace UMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserSyncService _userSyncService;

        public UsersController(ApplicationDbContext context, IUserSyncService userSyncService)
        {
            _context = context;
            _userSyncService = userSyncService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserModel model)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                CreatedDate = DateTime.UtcNow,
                 
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _userSyncService.SyncUserById(user.Id);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, CreateUserModel model)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Username = model.Username;
            user.Email = model.Email;
            user.FullName = model.FullName;

            await _context.SaveChangesAsync();
            await _userSyncService.SyncUserById(user.Id);

            return Ok(user);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await _userSyncService.SyncUserById(id);

            return NoContent();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserSyncService _userSyncService;

        public RolesController(ApplicationDbContext context, IUserSyncService userSyncService)
        {
            _context = context;
            _userSyncService = userSyncService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(Role model)
        {
            _context.Roles.Add(model);
            await _context.SaveChangesAsync();
            // Sync all users in this role
            foreach (var ur in model.UserRoles ?? Enumerable.Empty<UserRole>())
                await _userSyncService.SyncUserById(ur.UserId);

            return CreatedAtAction(nameof(GetRole), new { id = model.Id }, model);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();
            return Ok(role);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateRole(int id, Role model)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();

            role.Name = model.Name;
            await _context.SaveChangesAsync();
            foreach (var ur in role.UserRoles ?? Enumerable.Empty<UserRole>())
                await _userSyncService.SyncUserById(ur.UserId);

            return Ok(role);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.Include(r => r.UserRoles).FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return NotFound();

            var userIds = role.UserRoles?.Select(ur => ur.UserId).ToList() ?? new List<Guid>();
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return NoContent();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserSyncService _userSyncService;

        public PermissionsController(ApplicationDbContext context, IUserSyncService userSyncService)
        {
            _context = context;
            _userSyncService = userSyncService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermission(Permission model)
        {
            _context.Permissions.Add(model);
            await _context.SaveChangesAsync();
            // Sync all users in roles with this permission
            var userIds = _context.RolePermission
                .Where(rp => rp.PermissionId == model.Id)
                .SelectMany(rp => rp.Role.UserRoles.Select(ur => ur.UserId))
                .Distinct()
                .ToList();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return CreatedAtAction(nameof(GetPermission), new { id = model.Id }, model);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();
            return Ok(permission);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePermission(int id, Permission model)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();

            permission.Name = model.Name;
            await _context.SaveChangesAsync();
            var userIds = _context.RolePermission
                .Where(rp => rp.PermissionId == permission.Id)
                .SelectMany(rp => rp.Role.UserRoles.Select(ur => ur.UserId))
                .Distinct()
                .ToList();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return Ok(permission);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null) return NotFound();

            var userIds = _context.RolePermission
                .Where(rp => rp.PermissionId == id)
                .SelectMany(rp => rp.Role.UserRoles.Select(ur => ur.UserId))
                .Distinct()
                .ToList();

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return NoContent();
        }


    }

    [ApiController]
    [Route("api/[controller]")]
    public class UserRolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserSyncService _userSyncService;

        public UserRolesController(ApplicationDbContext context, IUserSyncService userSyncService)
        {
            _context = context;
            _userSyncService = userSyncService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserRole(Guid userId, int roleId)
        {
            // Find user and role from database
            var user = await _context.Users.FindAsync(userId);
            var role = await _context.Roles.FindAsync(roleId);

            if (user == null || role == null)
                return BadRequest("User or Role not found.");

            // Check if the UserRole already exists
            var existingUserRole = await _context.UserRole
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingUserRole != null)
                return Conflict("UserRole already exists.");

            // Create new UserRole
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                User = user,
                Role = role
            };

            _context.UserRole.Add(userRole);
            await _context.SaveChangesAsync();
            await _userSyncService.SyncUserById(userId);

            return CreatedAtAction(nameof(GetUserRole), new { userId = userId, roleId = roleId }, userRole);
        }

        [HttpGet("{userId:guid}/{roleId:int}")]
        public async Task<IActionResult> GetUserRole(Guid userId, int roleId)
        {
            var userRole = await _context.UserRole
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole == null) return NotFound();
            return Ok(userRole);
        }

        [HttpPut("{userId:guid}/{roleId:int}")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, int roleId)
        {
            var userRole = await _context.UserRole
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole == null) return NotFound();

            userRole.UserId = userId;
            userRole.RoleId = roleId;
            await _context.SaveChangesAsync();
            await _userSyncService.SyncUserById(userRole.UserId);
            return Ok(userRole);
        }

        [HttpDelete("{userId:guid}/{roleId:int}")]
        public async Task<IActionResult> DeleteUserRole(Guid userId, int roleId)
        {
            var userRole = await _context.UserRole
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole == null) return NotFound();

            _context.UserRole.Remove(userRole);
            await _context.SaveChangesAsync();
            await _userSyncService.SyncUserById(userId);
            return NoContent();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserSyncService _userSyncService;

        public RolePermissionsController(ApplicationDbContext context, IUserSyncService userSyncService)
        {
            _context = context;
            _userSyncService = userSyncService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRolePermission(RolePermission model)
        {
            _context.RolePermission.Add(model);
            await _context.SaveChangesAsync();
            // Sync all users in this role
            var userIds = _context.UserRole
                .Where(ur => ur.RoleId == model.RoleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToList();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return CreatedAtAction(nameof(GetRolePermission), new { roleId = model.RoleId, permissionId = model.PermissionId }, model);
        }

        [HttpGet("{roleId:int}/{permissionId:int}")]
        public async Task<IActionResult> GetRolePermission(int roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermission
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (rolePermission == null) return NotFound();
            return Ok(rolePermission);
        }

        [HttpPut("{roleId:int}/{permissionId:int}")]
        public async Task<IActionResult> UpdateRolePermission(int roleId, int permissionId, RolePermission model)
        {
            var rolePermission = await _context.RolePermission
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (rolePermission == null) return NotFound();

            rolePermission.RoleId = model.RoleId;
            rolePermission.PermissionId = model.PermissionId;
            await _context.SaveChangesAsync();
            var userIds = _context.UserRole
                .Where(ur => ur.RoleId == rolePermission.RoleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToList();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return Ok(rolePermission);
        }

        [HttpDelete("{roleId:int}/{permissionId:int}")]
        public async Task<IActionResult> DeleteRolePermission(int roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermission
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
            if (rolePermission == null) return NotFound();

            _context.RolePermission.Remove(rolePermission);
            await _context.SaveChangesAsync();
            var userIds = _context.UserRole
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToList();
            foreach (var userId in userIds)
                await _userSyncService.SyncUserById(userId);

            return NoContent();
        }
    }
}
