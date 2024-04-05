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

using Energinet.DataHub.EDI.B2CWebApi.Extensions.Options;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.Extensions.DependencyInjection;

public static class DateTimeExtensions
{
    public static IServiceCollection AddDateTime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DateTimeOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrEmpty(o.TIME_ZONE),
                "TIME_ZONE must be set");

        var options = configuration.Get<DateTimeOptions>()!;
        services.AddSingleton<DateTimeZone>(_ =>
        {
            var dateTimeZoneId = options.TIME_ZONE;
            return DateTimeZoneProviders.Tzdb.GetZoneOrNull(dateTimeZoneId)!;
        });

        return services;
    }
}
