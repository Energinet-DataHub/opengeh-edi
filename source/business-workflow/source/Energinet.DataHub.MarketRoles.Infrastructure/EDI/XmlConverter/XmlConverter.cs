// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Energinet.DataHub.MarketRoles.Application.Common;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter
{
    public class XmlConverter : IXmlConverter
    {
        private readonly XmlMapper _xmlMapper;

        public XmlConverter(XmlMapper xmlMapper)
        {
            _xmlMapper = xmlMapper;
        }

        public async Task<IEnumerable<IBusinessRequest>> DeserializeAsync(Stream body)
        {
            XElement rootElement = await XElement.LoadAsync(body, LoadOptions.None, CancellationToken.None);
            return _xmlMapper.Map(rootElement);
        }
    }
}
