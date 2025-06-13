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

using System.Text.RegularExpressions;

namespace Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;

public static partial class JsonFromXmlFieldExtractor
{
    public static string ExtractJsonFromXmlCData(string peekedMessageContent)
    {
        var match = MatchFirstMessageContentRegex().Match(peekedMessageContent);
        var jsonString = match.Success ? match.Groups[1].Captures[0].Value
            : throw new InvalidOperationException($"Could not parse '<ns0:CData>' from peeked message with reference: {
                MatchFirstMessageReferenceRegex().Match(peekedMessageContent).Groups[1].Captures[0]}");

        return jsonString;
    }

    [GeneratedRegex("(?:<ns0:CData>)(.*?)(<\\/ns0:CData>)")]
    private static partial Regex MatchFirstMessageContentRegex();

    [GeneratedRegex("(?:<ns0:MessageReference>)(.*?)(<\\/ns0:MessageReference>)")]
    private static partial Regex MatchFirstMessageReferenceRegex();
}
