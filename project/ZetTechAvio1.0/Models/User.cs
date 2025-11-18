using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZetTechAvio1._0.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(512)]
        public string PasswordHash { get; set; } = "";

        [StringLength(255)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum UserRole
    {
        Admin,
        Manager,
        User
    }
}
