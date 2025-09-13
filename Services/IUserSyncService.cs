using Microsoft.EntityFrameworkCore;
using Nest;
using UMS.Models.Documents;
using UMS.Models.Entities;

namespace UMS.Services
{
    public interface IUserSyncService
    {
        Task SyncUserById(Guid userId);
    }

    // UserSyncService.cs
    public class UserSyncService : IUserSyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly IElasticClient _elasticClient;
        private const string IndexName = "users"; // Tên index trên Elasticsearch

        public UserSyncService(ApplicationDbContext context, IElasticClient elasticClient)
        {
            _context = context;
            _elasticClient = elasticClient;
        }

        public async Task SyncUserById(Guid userId)
        {
            // 1. Lấy dữ liệu đầy đủ từ SQL Server
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                // Nếu user bị xóa, cũng nên xóa document trên Elasticsearch
                await _elasticClient.DeleteAsync<UserDocument>(userId, d => d.Index(IndexName));
                return;
            }

            // 2. Transform thành UserDocument
            var userDocument = new UserDocument
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                CreatedDate = user.CreatedDate,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList(),
                Permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
                    .Distinct()
                    .ToList()
            };

            // 3. Index (thêm mới hoặc cập nhật) vào Elasticsearch
            var response = await _elasticClient.IndexAsync(userDocument, i => i.Index(IndexName).Id(userDocument.Id));

            if (!response.IsValid)
            {
                throw new Exception("Failed to index user document");
            }
        }
    }
}
