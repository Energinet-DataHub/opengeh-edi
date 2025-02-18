// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
//
// using System.Diagnostics.CodeAnalysis;
// using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
// using Energinet.DataHub.EDI.SubsystemTests.Drivers;
// using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;
// using Energinet.DataHub.EDI.SubsystemTests.Dsl;
// using Energinet.DataHub.EDI.SubsystemTests.TestOrdering;
// using FluentAssertions;
// using Xunit.Abstractions;
// using Xunit.Categories;
//
// namespace Energinet.DataHub.EDI.SubsystemTests.Tests;
//
// [SuppressMessage(
//     "Usage",
//     "CA2007",
//     Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]
// [TestCaseOrderer(
//     ordererTypeName: "Energinet.DataHub.EDI.SubsystemTests.TestOrdering.TestOrderer",
//     ordererAssemblyName: "Energinet.DataHub.Wholesale.SubsystemTests")]
// [IntegrationTest]
// [Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
//
// // TODO: Rename this to brs026 when we have deleted the old request tests
// public sealed class WhenEnergyResultRequestedProcessManagerTests : BaseTestClass
// {
//     private readonly NotifyAggregatedMeasureDataResultDsl _notifyAggregatedMeasureDataResult;
//     private readonly AggregatedMeasureDataRequestDsl _aggregatedMeasureDataRequest;
//
//     public WhenEnergyResultRequestedProcessManagerTests(SubsystemTestFixture fixture, ITestOutputHelper output)
//         : base(output, fixture)
//     {
//         ArgumentNullException.ThrowIfNull(fixture);
//
//         var ediDriver = new EdiDriver(fixture.DurableClient, fixture.B2BClients.EnergySupplier, output);
//         var wholesaleDriver = new WholesaleDriver(fixture.EventPublisher, fixture.EdiInboxClient);
//
//         _notifyAggregatedMeasureDataResult = new NotifyAggregatedMeasureDataResultDsl(
//             ediDriver,
//             wholesaleDriver);
//
//         _aggregatedMeasureDataRequest =
//             new AggregatedMeasureDataRequestDsl(
//                 ediDriver,
//                 new B2CEdiDriver(fixture.B2CClients.EnergySupplier, fixture.ApiManagementUri, fixture.EdiB2CWebApiUri, output),
//                 new EdiDatabaseDriver(fixture.ConnectionString),
//                 wholesaleDriver,
//                 new ProcessManagerDriver(fixture.EdiTopicClient));
//     }
//
//     [Fact]
//     [Order(100)] // Default is 0, hence we assign this a higher number => it will run last, and therefor not interfere with the other tests
//     public async Task B2B_actor_can_request_aggregated_measure_data()
//     {
//         var act = async () => await _aggregatedMeasureDataRequest.Request(CancellationToken.None);
//
//         await act.Should().NotThrowAsync("because the request should be valid");
//     }
//
//     [Fact]
//     [Order(100)] // Default is 0, hence we assign this a higher number => it will run last, and therefor not interfere with the other tests
//     public async Task B2C_actor_can_request_aggregated_measure_data()
//     {
//         var act = async () => await _aggregatedMeasureDataRequest.B2CRequest(CancellationToken.None);
//
//         await act.Should().NotThrowAsync("because the request should be valid");
//     }
//
//     [Fact]
//     public async Task Actor_get_bad_request_when_aggregated_measure_data_request_is_invalid()
//     {
//         await _aggregatedMeasureDataRequest.ConfirmInvalidRequestIsRejected(CancellationToken.None);
//     }
//
//     [Fact]
//     public async Task Actor_can_peek_and_dequeue_response_from_aggregated_measure_data_request()
//     {
//         await _aggregatedMeasureDataRequest.PublishAcceptedBrs026RequestAsync(
//             "804",
//             new Actor(ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber), ActorRole.EnergySupplier));
//
//         await _notifyAggregatedMeasureDataResult.ConfirmResultIsAvailable();
//     }
//
//     [Fact]
//     public async Task Actor_can_peek_and_dequeue_rejected_response_from_aggregated_measure_data_request()
//     {
//         await _aggregatedMeasureDataRequest.PublishRejectedBrs026RequestAsync(
//             new Actor(ActorNumber.Create(SubsystemTestFixture.EdiSubsystemTestCimEnergySupplierNumber), ActorRole.EnergySupplier));
//
//         await _notifyAggregatedMeasureDataResult.ConfirmRejectResultIsAvailable();
//     }
// }
