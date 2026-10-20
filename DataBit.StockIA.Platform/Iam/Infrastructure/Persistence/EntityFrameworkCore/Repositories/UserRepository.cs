using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Context.Set<User>().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, int? exceptUserId = null, CancellationToken cancellationToken = default) =>
        Context.Set<User>().AnyAsync(user => user.Email == email && (exceptUserId == null || user.Id != exceptUserId), cancellationToken);

    public async Task<IEnumerable<User>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await Context.Set<User>().Where(user => user.RestaurantId == restaurantId).OrderByDescending(user => user.Id)
            .ToListAsync(cancellationToken);

    public Task<int> CountAdminsAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        Context.Set<User>().CountAsync(user => user.RestaurantId == restaurantId && user.Role == UserRole.Admin, cancellationToken);
}
