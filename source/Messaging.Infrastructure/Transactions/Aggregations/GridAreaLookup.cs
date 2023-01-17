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
using System.Threading.Tasks;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Actors;

namespace Messaging.Infrastructure.Transactions.Aggregations;

public class GridAreaLookup : IGridAreaLookup
{
    private readonly Dictionary<string, ActorNumber> _gridAreas = new()
    {
        { "003", ActorNumber.Create("5790002606892") },
        { "007", ActorNumber.Create("5790002606892") },
        { "014", ActorNumber.Create("5790001095253") },
        { "015", ActorNumber.Create("5790001095246") },
        { "016", ActorNumber.Create("5790002502699") },
        { "031", ActorNumber.Create("5790000610877") },
        { "042", ActorNumber.Create("5790000681075") },
        { "044", ActorNumber.Create("5790001089030") },
        { "051", ActorNumber.Create("5790001095277") },
        { "052", ActorNumber.Create("5790001089030") },
        { "084", ActorNumber.Create("5790001095239") },
        { "085", ActorNumber.Create("5790001103460") },
        { "131", ActorNumber.Create("5790001089030") },
        { "141", ActorNumber.Create("5790001090166") },
        { "142", ActorNumber.Create("5790001095338") },
        { "149", ActorNumber.Create("5790001089030") },
        { "151", ActorNumber.Create("5790000704842") },
        { "152", ActorNumber.Create("5790000681372") },
        { "154", ActorNumber.Create("5790001100520") },
        { "233", ActorNumber.Create("5790000610099") },
        { "244", ActorNumber.Create("5790000392261") },
        { "245", ActorNumber.Create("5790000683345") },
        { "246", ActorNumber.Create("5790000682225") },
        { "312", ActorNumber.Create("5790000375318") },
        { "331", ActorNumber.Create("5790000681358") },
        { "341", ActorNumber.Create("5790000681105") },
        { "342", ActorNumber.Create("5790000682102") },
        { "344", ActorNumber.Create("5790000611003") },
        { "347", ActorNumber.Create("5790000395620") },
        { "348", ActorNumber.Create("5790000681327") },
        { "351", ActorNumber.Create("5790001090111") },
        { "353", ActorNumber.Create("5790001089030") },
        { "357", ActorNumber.Create("5790001088309") },
        { "370", ActorNumber.Create("5790001095451") },
        { "371", ActorNumber.Create("5790001095376") },
        { "381", ActorNumber.Create("5790000610839") },
        { "384", ActorNumber.Create("5790000706419") },
        { "385", ActorNumber.Create("5790000610822") },
        { "394", ActorNumber.Create("5790001095345") },
        { "396", ActorNumber.Create("5790001095444") },
        { "398", ActorNumber.Create("5790001095413") },
        { "512", ActorNumber.Create("5790001087975") },
        { "531", ActorNumber.Create("5790000836727") },
        { "532", ActorNumber.Create("5790001088217") },
        { "533", ActorNumber.Create("5790000392551") },
        { "543", ActorNumber.Create("5790000610976") },
        { "552", ActorNumber.Create("5790001088187") },
        { "553", ActorNumber.Create("5790000683321") },
        { "554", ActorNumber.Create("5790000610181") },
        { "584", ActorNumber.Create("5790001089023") },
        { "587", ActorNumber.Create("5790001089108") },
        { "588", ActorNumber.Create("5790001090074") },
        { "590", ActorNumber.Create("5790001090081") },
        { "592", ActorNumber.Create("5790001089313") },
        { "651", ActorNumber.Create("5790001330439") },
        { "652", ActorNumber.Create("5790000705184") },
        { "740", ActorNumber.Create("5790000705184") },
        { "757", ActorNumber.Create("5790000836239") },
        { "791", ActorNumber.Create("5790000705689") },
        { "803", ActorNumber.Create("8100000000030") },
        { "804", ActorNumber.Create("8100000000047") },
        { "805", ActorNumber.Create("8200000007739") },
        { "806", ActorNumber.Create("8200000007746") },
        { "853", ActorNumber.Create("5790001088460") },
        { "854", ActorNumber.Create("5790001088231") },
        { "860", ActorNumber.Create("5790001089375") },
        { "911", ActorNumber.Create("5790000706686") },
        { "920", ActorNumber.Create("5799994000107") },
        { "950", ActorNumber.Create("5790002606892") },
        { "951", ActorNumber.Create("5790002606892") },
        { "952", ActorNumber.Create("5790002606892") },
        { "953", ActorNumber.Create("5790002606892") },
        { "960", ActorNumber.Create("5790002606892") },
        { "961", ActorNumber.Create("5790002606892") },
        { "962", ActorNumber.Create("5790002606892") },
        { "980", ActorNumber.Create("5790001265472") },
        { "981", ActorNumber.Create("5790001417628") },
        { "982", ActorNumber.Create("5790002606892") },
        { "983", ActorNumber.Create("7300009004287") },
        { "990", ActorNumber.Create("4260024590017") },
        { "991", ActorNumber.Create("7331507000006") },
        { "992", ActorNumber.Create("4033872000010") },
        { "993", ActorNumber.Create("7080000923168") },
        { "995", ActorNumber.Create("9910891000005") },
        { "996", ActorNumber.Create("7331507000006") },
        { "997", ActorNumber.Create("7359991140008") },
        { "999", ActorNumber.Create("5790002606892") },
    };

    public Task<ActorNumber> GetGridOperatorForAsync(string gridAreaCode)
    {
        return Task.FromResult(_gridAreas[gridAreaCode]);
    }
}
