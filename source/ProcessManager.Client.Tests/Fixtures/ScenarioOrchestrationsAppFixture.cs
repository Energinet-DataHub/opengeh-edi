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
// using Energinet.DataHub.ProcessManager.Orchestrations.Tests.Fixtures;
//
// namespace Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;
//
// /// <summary>
// /// Configure fixture for scenario testing Process Manager Client,
// /// which requires both Orchestrations app and Process Manager app
// /// to run simultaneously with coordinated configuration.
// /// </summary>
// public class ScenarioOrchestrationsAppFixture
//     : OrchestrationsAppFixtureBase
// {
//     /// <summary>
//     /// See details at <see cref="ScenarioAppFixturesConfiguration"/>.
//     /// </summary>
//     public ScenarioOrchestrationsAppFixture()
//         : base(
//             ScenarioAppFixturesConfiguration.Instance.DatabaseManager,
//             ScenarioAppFixturesConfiguration.Instance.TaskHubName,
//             ScenarioAppFixturesConfiguration.Instance.OrchestrationsAppPort,
//             disposeDatabase: true) // Quickfix: Only dispose database in one of the scenario fixtures
//     {
//     }
// }
