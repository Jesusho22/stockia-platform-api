using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;

namespace DataBit.StockIA.Platform.Iam.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, int? exceptUserId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> FindAllByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<int> CountAdminsAsync(int restaurantId, CancellationToken cancellationToken = default);
}
