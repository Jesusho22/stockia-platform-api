using DataBit.StockIA.Platform.Iam.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.Iam.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace DataBit.StockIA.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class RestaurantRepository(AppDbContext context) : BaseRepository<Restaurant>(context), IRestaurantRepository
{
}
