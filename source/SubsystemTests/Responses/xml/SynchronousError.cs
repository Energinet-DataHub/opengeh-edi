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

using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Energinet.DataHub.EDI.AcceptanceTests.Responses.Xml;

[SuppressMessage("Security", "CA5369:Use XmlReader for \'XmlSerializer.Deserialize()\'", Justification = "Not available through API")]
public static class SynchronousError
{
    public static ErrorResponse? BuildB2BErrorResponse(string responseData)
    {
        var serializer = new XmlSerializer(typeof(ErrorResponse));
        ErrorResponse error;

        using (var reader = new StringReader(responseData))
        {
            error = (ErrorResponse)serializer.Deserialize(reader)!;
        }

        return error;
    }
}

[XmlRoot(ElementName = "Error")]
public class ErrorResponse
{
    [XmlElement(ElementName = "Code")]
    public string Code { get; set; }

    [XmlElement(ElementName = "Message")]
    public string Message { get; set; }

    [XmlElement(ElementName = "Target")]
    public string Target { get; set; }

    [XmlElement(ElementName = "Details")]
    public Details Details { get; set; }
}

[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Deserializer needs to write")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Deserializer needs to be able to set it")]
public class Details
{
    [XmlElement(ElementName = "Error")]
    public List<InnerError> InnerErrors { get; set; }
}

public class InnerError
{
    [XmlElement(ElementName = "Code")]
    public string Code { get; set; }

    [XmlElement(ElementName = "Message")]
    public string Message { get; set; }

    [XmlElement(ElementName = "Target")]
    public string Target { get; set; }
}
