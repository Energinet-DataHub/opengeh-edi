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

using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.SeedWork;
using Domain.Transactions.AggregatedMeasureData.Events;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using NodaTime;
using NodaTime.Text;
using Period = Domain.Transactions.Aggregations.Period;
using Point = Domain.Transactions.Aggregations.Point;
using wholesaleSerie = Energinet.DataHub.Edi.Responses.Serie;

namespace Domain.Transactions.AggregatedMeasureData
{
    public class AggregatedMeasureDataProcess : Entity
    {
        private readonly ActorNumber _requestedByActorId;
        private State _state;

        public AggregatedMeasureDataProcess(
            ProcessId processId,
            BusinessTransactionId businessTransactionId,
            ActorNumber requestedByActorId,
            string? settlementVersion,
            string? meteringPointType,
            string? settlementMethod,
            Instant startOfPeriod,
            Instant? endOfPeriod,
            string? meteringGridAreaDomainId,
            string? energySupplierId,
            string? balanceResponsibleId)
        {
            ProcessId = processId;
            BusinessTransactionId = businessTransactionId;
            SettlementVersion = settlementVersion;
            MeteringPointType = meteringPointType;
            SettlementMethod = settlementMethod;
            StartOfPeriod = startOfPeriod;
            EndOfPeriod = endOfPeriod;
            MeteringGridAreaDomainId = meteringGridAreaDomainId;
            EnergySupplierId = energySupplierId;
            BalanceResponsibleId = balanceResponsibleId;
            _state = State.Initialized;
            _requestedByActorId = requestedByActorId;
            AddDomainEvent(new AggregatedMeasureProcessWasStarted(ProcessId));
        }

        public enum State
        {
            Initialized,
            Sent,
            Accepted, // TODO: LRN this would indicate that the process is completed, is only property to  describe state enough?
            Rejected,
        }

        public ProcessId ProcessId { get; }

        public BusinessTransactionId BusinessTransactionId { get; }

        /// <summary>
        /// Represent the version for a specific calculation.
        /// </summary>
        public string? SettlementVersion { get; }

        /// <summary>
        /// Represent consumption types or production.
        /// </summary>
        public string? MeteringPointType { get; }

        /// <summary>
        /// Represent the type of Settlement. E.g. Flex or NonProfile or null
        /// </summary>
        public string? SettlementMethod { get; }

        public Instant StartOfPeriod { get; }

        public Instant? EndOfPeriod { get; }

        public string? MeteringGridAreaDomainId { get; }

        public string? EnergySupplierId { get; }

        public string? BalanceResponsibleId { get; }

        public void WholesaleIsNotifiedOfRequest()
        {
            if (_state == State.Sent)
            {
                throw new AggregatedMeasureDataException("Wholesale has already been notified");
            }

            _state = State.Sent;
        }

        public void ReplyFromWholesaleAccepted()
        {
            if (_state == State.Accepted)
            {
                return;
            }

            if (_state != State.Sent)
            {
                throw new AggregatedMeasureDataException("Wholesale has not been notified yet");
            }

            _state = State.Accepted;
        }

        public bool HasWholesaleAlreadyReplied()
        {
            return _state == State.Accepted || _state == State.Rejected;
        }

        public void CheckThatProcessReadyForWholesaleReply()
        {
            if (_state == State.Initialized)
            {
                throw new AggregatedMeasureDataException("Wholesale has not been notified yet");
            }
        }

        // TODO: this should live inside infrastructure instead of domain
#pragma warning disable CA1002
        public List<AggregationResultMessage> CreateMessage(AggregatedTimeSeriesRequestAccepted timeseries)
#pragma warning restore CA1002
        {
            ArgumentNullException.ThrowIfNull(timeseries);
            var aggregations = AggregationFromTimeSeries(timeseries.Series.ToList());

            // TransactionId and processId is the same.
            var processId = TransactionId.Create(ProcessId.Id);

            var messages = aggregations.Select(aggregation =>
                AggregationResultMessage.Create(
                    _requestedByActorId,
                    MarketRole.GridOperator, //TODO: Change this
                    processId!,
                    aggregation)).ToList();
            return messages;
        }

        private static decimal DecimalFromDecimalValue(DecimalValue value)
        {
            const int nanoFactor = 1_000_000_000;
            return value.Units + (value.Nanos / nanoFactor);
        }

        private static string MapQuality(QuantityQuality quality)
        {
            return quality switch
            {
                QuantityQuality.Incomplete => Quality.Incomplete.Name,
                QuantityQuality.Measured => Quality.Measured.Name,
                QuantityQuality.Missing => Quality.Missing.Name,
                QuantityQuality.Estimated => Quality.Estimated.Name,
                QuantityQuality.Unspecified => throw new InvalidOperationException("Quality is not specified"),
                _ => throw new InvalidOperationException("Unknown quality type"),
            };
        }

        private static IReadOnlyList<Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
        {
            var points = new List<Point>();

            var pointPosition = 1;
            foreach (var point in timeSeriesPoints)
            {
                points.Add(new Point(pointPosition, DecimalFromDecimalValue(point.Quantity), MapQuality(point.QuantityQuality), point.Time?.ToString() ?? string.Empty));
                pointPosition++;
            }

            return points.AsReadOnly();
        }

        private static string MapUnitType(wholesaleSerie serie)
        {
            return serie.QuantityUnit switch
            {
                QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
                QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
                _ => throw new InvalidOperationException("Unknown unit type"),
            };
        }

        private static string MapResolution(wholesaleSerie serie)
        {
            return Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name;
        }

        private static Period MapPeriod(wholesaleSerie serie)
        {
            var startTime = serie.Period.StartOfPeriod;
            var endTime = serie.Period.EndOfPeriod;
            var hej = Instant.FromDateTimeUtc(endTime.ToDateTime());
            return new Period(
                Instant.FromDateTimeUtc(startTime.ToDateTime()),
                Instant.FromDateTimeUtc(endTime.ToDateTime()));
        }

        private static string? MapSettlementMethod(wholesaleSerie serie)
        {
            return SettlementType.Flex.Name;
            /*
            return serie.SettlementVersion switch // Fix this
            {
                nameof(TimeSeriesType.Production) => null,
                nameof(TimeSeriesType.FlexConsumption) => SettlementType.Flex.Name,
                nameof(TimeSeriesType.NonProfiledConsumption) => SettlementType.NonProfiled.Name,
                _ => null,
            };*/
        }

        private static string MapProcessType(wholesaleSerie serie)
        {
            return BusinessReason.PreliminaryAggregation.Name;
        }

        private List<Aggregation> AggregationFromTimeSeries(List<wholesaleSerie> timeSeries)
        {
            var temp = MapPeriod(timeSeries.First());

            var aggregations = timeSeries.Select(serie => new Aggregation(
                MapPoints(serie.TimeSeriesPoints),
                MapMeteringPointType(serie),
                MapUnitType(serie),
                MapResolution(serie), // as of right now, this does not exist
                MapPeriod(serie),
                MapSettlementMethod(serie),
                MapProcessType(serie),
                MapActorGrouping(serie),
                MapGridAreaDetails(serie)));

            return aggregations.ToList();
        }

#pragma warning disable CA1822 // remove this when we have proper data
        private string MapMeteringPointType(wholesaleSerie serie)
#pragma warning restore CA1822
        {
            return Domain.OutgoingMessages.MeteringPointType.Production.Name;
            /*
            return MeteringPointType switch
            {
                nameof(TimeSeriesType.Production) => Domain.OutgoingMessages.MeteringPointType.Production.Name,
                nameof(TimeSeriesType.FlexConsumption) => Domain.OutgoingMessages.MeteringPointType.Consumption.Name,
                nameof(TimeSeriesType.NonProfiledConsumption) => Domain.OutgoingMessages.MeteringPointType.Consumption.Name,
                nameof(TimeSeriesType.NetExchangePerGa) => Domain.OutgoingMessages.MeteringPointType.Exchange.Name,
                nameof(TimeSeriesType.NetExchangePerNeighboringGa) => Domain.OutgoingMessages.MeteringPointType.Exchange.Name,
                nameof(TimeSeriesType.TotalConsumption) => Domain.OutgoingMessages.MeteringPointType.Consumption.Name,
                nameof(TimeSeriesType.Unspecified) => throw new InvalidOperationException("Unknown metering point type"),
                _ => throw new InvalidOperationException("Could not determine metering point type"),
            };*/
        }

        private ActorGrouping MapActorGrouping(wholesaleSerie serie)
        {
            return new ActorGrouping(EnergySupplierId, BalanceResponsibleId);
        }

        private GridAreaDetails MapGridAreaDetails(wholesaleSerie serie)
        {
            // correct this
            return new GridAreaDetails(MeteringGridAreaDomainId ?? string.Empty, "5790002606892");
        }
    }
}
