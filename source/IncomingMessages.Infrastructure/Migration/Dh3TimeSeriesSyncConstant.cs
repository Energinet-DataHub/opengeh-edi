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

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;

public static class Dh3TimeSeriesSyncConstant
{
    public const string MigrationPath = "TSSYNC";
    public const string MeteringPoint = "metering_point";
    public const string TimeSeries = "time_series";
    public const string MeteringPointId = "metering_point_id";
    public const string MasterData = "masterdata";
    public const string GridArea = "grid_area";
    public const string TypeOfMp = "type_of_mp";
    public const string MasterDataStartDate = "masterdata_start_date";
    public const string MasterDataEndDate = "masterdata_end_date";
    public const string TransactionId = "transaction_id";
    public const string MessageId = "message_id";
    public const string ValidFromDate = "valid_from_date";
    public const string ValidToDate = "valid_to_date";
    public const string TransactionInsertDate = "transaction_insert_date";
    public const string HistoricalFlag = "historical_flag";
    public const string Resolution = "resolution";
    public const string Unit = "unit";
    public const string Status = "status";
    public const string ReadReason = "read_reason";
    public const string Values = "values";
    public const string Position = "position";
    public const string Quantity = "quantity";
    public const string Quality = "quality";
}
