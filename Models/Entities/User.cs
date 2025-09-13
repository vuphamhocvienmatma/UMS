namespace UMS.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public DateTime CreatedDate { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
    }
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
    }
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
    }

    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public int PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}
