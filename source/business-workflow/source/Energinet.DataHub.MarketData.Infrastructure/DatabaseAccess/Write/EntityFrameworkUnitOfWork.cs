using System.Threading.Tasks;

namespace Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write
{
    public class EntityFrameworkUnitOfWork : IUnitOfWork
    {
        private readonly WriteDatabaseContext _databaseContext;

        public EntityFrameworkUnitOfWork(WriteDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        public async Task CommitAsync()
        {
            await _databaseContext.SaveChangesAsync();
        }
    }
}
