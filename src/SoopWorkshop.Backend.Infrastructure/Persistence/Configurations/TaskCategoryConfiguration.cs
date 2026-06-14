using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Configurations
{
    public class TaskCategoryConfiguration : IEntityTypeConfiguration<TaskCategory>
    {

        public void Configure(EntityTypeBuilder<TaskCategory> entityTypeBuilder)
        {
            entityTypeBuilder.HasKey(c => c.Id);

            entityTypeBuilder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entityTypeBuilder.HasMany(c => c.Tasks)
                .WithOne(t => t.Category)
                .HasForeignKey(t => t.TaskCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}