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

using System;
using Newtonsoft.Json.Linq;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests;

public class AssertMarketActivityRecord
{
    private readonly JToken _payload;

    public AssertMarketActivityRecord(string payload)
    {
        _payload = JToken.Parse(payload);
    }

    public AssertMarketActivityRecord HasId()
    {
        Assert.NotNull(_payload.Value<string>("Id"));
        return this;
    }

    public AssertMarketActivityRecord HasOriginalTransactionId(string originalTransactionId)
    {
        Assert.Equal(originalTransactionId, _payload.Value<string>("OriginalTransactionId"));
        return this;
    }

    public AssertMarketActivityRecord HasMarketEvaluationPointId(string marketEvaluationPointId)
    {
        Assert.Equal(marketEvaluationPointId, _payload.Value<string>("MarketEvaluationPointId"));
        return this;
    }

    public AssertMarketActivityRecord HasValidityStart(DateTime validityStart)
    {
        Assert.Equal(validityStart, _payload.Value<DateTime>("ValidityStart"));
        return this;
    }

    public AssertMarketActivityRecord HasValidityStart(Instant validityStart)
    {
        Assert.Equal(validityStart.ToDateTimeUtc(), _payload.Value<DateTime>("ValidityStart"));
        return this;
    }

    public AssertMarketActivityRecord NotEmpty(string property)
    {
        Assert.NotEmpty(_payload.Value<string>(property));
        return this;
    }

    public AssertMarketActivityRecord HasValue<T>(string path, T expectedValue)
    {
        Assert.Equal(expectedValue, _payload.Value<T>(path));
        return this;
    }

    public AssertMarketActivityRecord HasDateValue(string path, Instant expectedValue)
    {
        Assert.Equal(expectedValue.ToDateTimeUtc(), _payload.SelectToken(path).Value<DateTime>());
        return this;
    }

    public AssertMarketActivityRecord HasMarketEvaluationPointDateValue(string path, Instant expectedValue)
    {
        Assert.Equal(expectedValue.ToDateTimeUtc(), _payload.SelectToken($"MarketEvaluationPoint.{path}").Value<DateTime>());
        return this;
    }

    public AssertMarketActivityRecord HasMarketEvaluationPointValue<T>(string path, T expectedValue)
    {
        Assert.Equal(expectedValue, _payload.SelectToken($"MarketEvaluationPoint.{path}").Value<T>());
        return this;
    }
}
