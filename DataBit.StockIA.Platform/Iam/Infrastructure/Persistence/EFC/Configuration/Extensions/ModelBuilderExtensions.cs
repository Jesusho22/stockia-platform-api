using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedOnAdd();
            entity.Property(user => user.FullName).IsRequired().HasMaxLength(120);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(user => user.PasswordResetCodeHash).HasMaxLength(100);
            // The restaurant lives in Restaurant Registration: an id, not a cross-context foreign key.
            entity.HasIndex(user => user.RestaurantId);
            entity.Ignore(user => user.IsAdmin);
        });
    }
}
