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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

public static class WorkaroundFlags
{
    /// <summary>
    /// This is a workaround that always returns true, to be able to identify the places where the workaround/"hack"
    ///     for a MDR/GridOperator actor which is the same Actor but with two different roles MDR and GridOperator, is used.
    /// The hack is:
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    ///     but uses the DDM (GridOperator) role when peeking.
    /// This means that a AggregatedMeasureData document with a MDR receiver should be added to the DDM ActorMessageQueue
    ///     and when peeking as a MDR receiver you should peek into the DDM ActorMessageQueue
    /// </summary>
    public static bool MeteredDataResponsibleToGridOperatorHack => true;

    /// <summary>
    /// This is a workaround that always returns true, to be able to identify the places where the workaround/"hack"
    /// for an MDR/GridOperator actor which is the same Actor but with two different roles MDR and GridOperator, is used.
    /// The hack is:
    /// To enable a DDM to RequestAggregatedMeasuredData (RSM-016) we change the role the actor is using to MeterDataResponsible (MDR)
    /// A request will result in a message with the SenderRole being MDR, but the ´MeteredDataResponsibleToGridOperatorHack´ will ensure
    /// the message will be delivered to the actors DDM ActorMessageQueue. This way when the Actor then Peeks the queue using either
    /// the DDM or the MDR role they will be able to get the result of the request.
    /// </summary>
    public static bool GridOperatorToMeteredDataResponsibleHack => true;
}
