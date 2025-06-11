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

using Energinet.DataHub.EDI.B2CWebApi.Models.V1;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using NodaTime;
using NodaTime.Extensions;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Quality = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Quality;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories.V1;

public class SendMeasurementsDtoFactory(
    IClock clock)
{
    private readonly IClock _clock = clock;

    public SendMeasurementsDto CreateDto(
        Actor sender,
        SendMeasurementsRequestV1 request)
    {
        return new SendMeasurementsDto(
            MessageId: MessageId.New(),
            TransactionId: TransactionId.New(),
            MeteringPointId: MeteringPointId.From(request.MeteringPointId),
            MeteringPointType: MapMeteringPointType(request.MeteringPointType),
            Sender: sender,
            CreatedAt: _clock.GetCurrentInstant(),
            Resolution: MapResolution(request.Resolution),
            Start: request.Start.ToInstant(),
            End: request.End.ToInstant(),
            Measurements: request.Measurements.Select(
                m => new SendMeasurementsDto.MeasurementDto(
                    m.Position,
                    m.Quantity,
                    MapQuality(request.Quality)))
                .ToList());
    }

    private static Resolution MapResolution(Models.V1.Resolution resolution) => resolution switch
    {
        Models.V1.Resolution.Hourly => Resolution.Hourly,
        Models.V1.Resolution.QuarterHourly => Resolution.QuarterHourly,
        _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Invalid resolution."),
    };

    private static MeteringPointType MapMeteringPointType(Models.V1.MeteringPointType type) => type switch
    {
        Models.V1.MeteringPointType.Production => MeteringPointType.Production,
        Models.V1.MeteringPointType.Consumption => MeteringPointType.Consumption,
        Models.V1.MeteringPointType.Exchange => MeteringPointType.Exchange,

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid metering point type."),
    };

    private static Quality MapQuality(Models.V1.Quality quality) => quality switch
    {
        Models.V1.Quality.Calculated => Quality.Calculated,
        Models.V1.Quality.Measured => Quality.Measured,
        _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Invalid quality."),
    };
}
