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

using Energinet.DataHub.EDI.B2CWebApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Energinet.DataHub.EDI.AcceptanceTests.TestData;

public static class ArchivedMessageData
{
    public static JObject GetSearchableDataObject(string messageId, string senderNumber, string receiverNumber, string documentTypes, string businessResaons)
    {
        var searchableDataObject = new
        {
            dateTimeFrom = DateTime.UtcNow.AddMinutes(-5).ToString("s") + "Z",
            dateTimeTo = DateTime.UtcNow.AddMinutes(5).ToString("s") + "Z",
            messageId,
            senderNumber,
            receiverNumber,
            documentTypes,
            businessResaons,
        };

        string jsonData = JsonConvert.SerializeObject(searchableDataObject);

        return JObject.Parse(jsonData);
    }
}
