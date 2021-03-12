﻿// Copyright 2020 Energinet DataHub A/S
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

using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.SoapAdapter.Application.Parsers;
using Energinet.DataHub.SoapAdapter.Domain.Validation;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.SoapAdapter.Application.Converters
{
    public class ChangeSupplierConverter : RsmConverter
    {
        private const string B2BNamespace = "un:unece:260:data:EEM-DK_RequestChangeOfSupplier:v3";

        protected override async ValueTask ConvertPayloadAsync(
            XmlReader reader,
            RsmHeader header,
            Utf8JsonWriter writer,
            string correlationId)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteStartArray();

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.Is("DK_RequestChangeOfSupplier", B2BNamespace, XmlNodeType.EndElement))
                {
                    break;
                }
                else if (reader.Is("PayloadMPEvent", B2BNamespace))
                {
                    await ProcessPayloadMpEventAsync(reader, header, writer, correlationId).ConfigureAwait(false);
                }
            }

            writer.WriteEndArray();
        }

        private static async ValueTask<string> GetChildIdentificationAsync(XmlReader reader)
        {
            if (reader.ReadToFollowing("Identification", B2BNamespace))
            {
                return await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }

            return string.Empty;
        }

        private static async ValueTask ProcessConsumerPartyAsync(
            XmlReader reader,
            RsmHeader header,
            Utf8JsonWriter writer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            string? cpr = null;
            string? cvr = null;
            string? qualifier = null;
            string? name = null;

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.Is("ConsumerConsumerParty", B2BNamespace, XmlNodeType.EndElement))
                {
                    break;
                }

                if (reader.Is("CPR", B2BNamespace) && cpr == null)
                {
                    cpr = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    qualifier = "ARR";
                }
                else if (reader.Is("CVR", B2BNamespace) && cvr == null)
                {
                    cvr = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    qualifier = "VA";
                }
                else if (reader.Is("Name", B2BNamespace))
                {
                    name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                }
            }

            if ((cvr != null && cpr != null) || (cvr == null && cpr == null))
            {
                throw new Exception("Inconsistency in CVR or CPR");
            }

            writer.WriteStartObject("Consumer");
            writer.WriteString("mRID", cvr ?? cpr);
            writer.WriteString("qualifier", qualifier);

            if (name != null)
            {
                writer.WriteString("name", name);
            }

            writer.WriteEndObject();
        }

        private static async ValueTask ProcessPayloadMpEventAsync(
            XmlReader reader,
            RsmHeader header,
            Utf8JsonWriter writer,
            string correlationId)
        {
            do
            {
                if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
                {
                    continue;
                }

                if (reader.Is("PayloadMPEvent", B2BNamespace))
                {
                    writer.WriteStartObject();
                    writer.WriteString("CorrelationId", correlationId);
                }
                else if (reader.Is("PayloadMPEvent", B2BNamespace, XmlNodeType.EndElement))
                {
                    writer.WriteEndObject();
                }
                else if (reader.Is("Identification", B2BNamespace))
                {
                    writer.WriteStartObject("Transaction");
                    writer.WriteString("mRID", await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                    writer.WriteEndObject();
                }
                else if (reader.Is("StartOfOccurrence", B2BNamespace))
                {
                    var instantResult = InstantPattern.ExtendedIso.Parse(
                        await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                    if (instantResult.Success)
                    {
                        writer.WriteString("StartDate", instantResult.Value.ToString());
                    }
                }
                else if (reader.Is("MeteringPointDomainLocation", B2BNamespace))
                {
                    writer.WriteStartObject("MarketEvaluationPoint");
                    writer.WriteString("mRID", await GetChildIdentificationAsync(reader).ConfigureAwait(false));
                    writer.WriteEndObject();
                }
                else if (reader.Is("BalanceSupplierEnergyParty", B2BNamespace))
                {
                    writer.WriteStartObject("EnergySupplier");
                    writer.WriteString("mRID", await GetChildIdentificationAsync(reader).ConfigureAwait(false));
                    writer.WriteEndObject();
                }
                else if (reader.Is("BalanceResponsiblePartyEnergyParty", B2BNamespace))
                {
                    writer.WriteStartObject("BalanceResponsibleParty");
                    writer.WriteString("mRID", await GetChildIdentificationAsync(reader).ConfigureAwait(false));
                    writer.WriteEndObject();
                }
                else if (reader.Is("ConsumerConsumerParty", B2BNamespace))
                {
                    await ProcessConsumerPartyAsync(reader, header, writer).ConfigureAwait(false);
                }
            }
            while (await reader.ReadAsync().ConfigureAwait(false));
        }
    }
}
