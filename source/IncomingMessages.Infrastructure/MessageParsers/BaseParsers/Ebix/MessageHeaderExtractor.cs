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

using System.Xml.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers.Ebix;

internal static class MessageHeaderExtractor
{
    private const string HeaderElementName = "HeaderEnergyDocument";
    private const string EnergyContextElementName = "ProcessEnergyContext";
    private const string Identification = "Identification";
    private const string DocumentType = "DocumentType";
    private const string Creation = "Creation";
    private const string SenderEnergyParty = "SenderEnergyParty";
    private const string RecipientEnergyParty = "RecipientEnergyParty";
    private const string EnergyBusinessProcess = "EnergyBusinessProcess";
    private const string EnergyBusinessProcessRole = "EnergyBusinessProcessRole";
    private const string EnergyIndustryClassification = "EnergyIndustryClassification";

    public static MessageHeader Extract(
        XDocument document,
        XNamespace ns)
    {
        var headerElement = document.Descendants(ns + HeaderElementName).SingleOrDefault();
        if (headerElement == null) throw new InvalidOperationException("Header element not found");

        var messageId = headerElement.Element(ns + Identification)?.Value ?? string.Empty;
        var messageType = headerElement.Element(ns + DocumentType)?.Value ?? string.Empty;
        var createdAt = headerElement.Element(ns + Creation)?.Value ?? string.Empty;
        var senderId = headerElement.Element(ns + SenderEnergyParty)?.Element(ns + Identification)?.Value ?? string.Empty;
        var receiverId = headerElement.Element(ns + RecipientEnergyParty)?.Element(ns + Identification)?.Value ?? string.Empty;

        var energyContextElement = document.Descendants(ns + EnergyContextElementName).FirstOrDefault();
        if (energyContextElement == null) throw new InvalidOperationException("Energy Context element not found");

        var businessReason = energyContextElement.Element(ns + EnergyBusinessProcess)?.Value ?? string.Empty;
        var senderRole = energyContextElement.Element(ns + EnergyBusinessProcessRole)?.Value ?? string.Empty;
        var businessType = energyContextElement.Element(ns + EnergyIndustryClassification)?.Value;

        return new MessageHeader(
            messageId,
            messageType,
            businessReason,
            senderId,
            senderRole,
            receiverId,
            // ReceiverRole is not specified in incoming Ebix documents
            ActorRole.MeteredDataAdministrator.Code,
            createdAt,
            businessType);
    }
}
