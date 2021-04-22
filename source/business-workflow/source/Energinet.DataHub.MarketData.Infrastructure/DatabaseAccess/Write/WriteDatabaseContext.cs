using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;

namespace Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write
{
    public class WriteDatabaseContext : BaseDatabaseContext, IWriteDatabaseContext
    {
        private readonly string _connectionString;

        public WriteDatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            return result;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString, x => x.UseNodaTime());
        }
    }
}
