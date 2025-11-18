using Microsoft.EntityFrameworkCore;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация таблицы Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("idx_email");

                entity.HasIndex(e => e.Role)
                    .HasDatabaseName("idx_role");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.FullName)
                    .HasMaxLength(255);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.Role)
                    .HasConversion<string>();

                entity.Property(e => e.CreatedAt);

                entity.Property(e => e.UpdatedAt);
            });
        }
    }
}
