using System.IO;
using System.Text;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Serialization
{
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
