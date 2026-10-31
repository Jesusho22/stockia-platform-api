using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.RestaurantRegistration.Domain.Repositories;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace DataBit.StockIA.Platform.RestaurantRegistration.Infrastructure.Persistence.EFC.Repositories;

public class RestaurantRepository(AppDbContext context) : BaseRepository<Restaurant>(context), IRestaurantRepository
{
}
