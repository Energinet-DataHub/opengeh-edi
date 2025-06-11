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

using System.Security.Claims;
using Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;
using Energinet.DataHub.EDI.B2CWebApi.Controllers;
using Energinet.DataHub.EDI.B2CWebApi.Models.V1;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using MeteringPointType = Energinet.DataHub.EDI.B2CWebApi.Models.V1.MeteringPointType;
using Quality = Energinet.DataHub.EDI.B2CWebApi.Models.V1.Quality;
using Resolution = Energinet.DataHub.EDI.B2CWebApi.Models.V1.Resolution;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Tests;

[Collection(nameof(B2CWebApiCollectionFixture))]
public class SendMeasurementsRequestTests : IAsyncLifetime
{
    private readonly B2CWebApiFixture _fixture;

    public SendMeasurementsRequestTests(B2CWebApiFixture fixture, ITestOutputHelper logger)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(logger);
    }

    private Claim[] RequiredActorClaims =>
    [
        new("actornumber", "1234567890123"),
        new("marketroles", ActorRole.MeteredDataResponsible.Name), // TODO #1670: Update to the correct role ?
    ];

    public static TheoryData<MeteringPointType, Resolution, Quality> RequestsWithAllResolutions()
    {
        var theoryData = new TheoryData<MeteringPointType, Resolution, Quality>();

        var ignoredResolutions = new[]
        {
            Resolution.Monthly, // Monthly resolution is not supported in Send Measurements
        };
        var resolutions = Enum.GetValues<Resolution>().Except(ignoredResolutions);

        foreach (var resolution in resolutions)
        {
            theoryData.Add(MeteringPointType.Consumption, resolution, Quality.Measured);
        }

        return theoryData;
    }

    public static TheoryData<MeteringPointType, Resolution, Quality> RequestsWithAllMeteringPointTypes()
    {
        var theoryData = new TheoryData<MeteringPointType, Resolution, Quality>();

        var meteringPointTypes = Enum.GetValues<MeteringPointType>();

        foreach (var meteringPointType in meteringPointTypes)
        {
            theoryData.Add(meteringPointType, Resolution.QuarterHourly, Quality.Measured);
        }

        return theoryData;
    }

    public static TheoryData<MeteringPointType, Resolution, Quality> RequestsWithAllQualities()
    {
        var theoryData = new TheoryData<MeteringPointType, Resolution, Quality>();

        var qualities = Enum.GetValues<Quality>();

        foreach (var quality in qualities)
        {
            theoryData.Add(MeteringPointType.Consumption, Resolution.QuarterHourly, quality);
        }

        return theoryData;
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null);
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(RequestsWithAllMeteringPointTypes))]
    [MemberData(nameof(RequestsWithAllResolutions))]
    [MemberData(nameof(RequestsWithAllQualities))]
    public async Task Given_SendMeasurementsRequest_When_ValidRequest_Then_Success(
        MeteringPointType meteringPointType,
        Resolution resolution,
        Quality quality)
    {
        // Arrange
        const int positions = 2;
        var resolutionAsDuration = resolution switch
        {
            Resolution.QuarterHourly => Duration.FromMinutes(15),
            Resolution.Hourly => Duration.FromHours(1),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unhandled resolution"),
        };

        var start = Instant.FromUtc(2025, 06, 11, 13, 00);
        var end = start.Plus(resolutionAsDuration * positions);

        var request = new SendMeasurementsRequestV1(
            MeteringPointId: "123456789012345678",
            MeteringPointType: meteringPointType,
            Resolution: resolution,
            Quality: quality,
            Start: start.ToDateTimeOffset(),
            End: end.ToDateTimeOffset(),
            Measurements:
            [
                // 2 positions, must match the "positions" const above
                new(1, 42.123m),
                new(2, 1337m),
            ]);

        using var httpRequest = B2CWebApiRequests.CreateSendMeasurementsV1Request(request);
        httpRequest.Headers.Authorization = await _fixture.OpenIdJwtManager.JwtProvider
            .CreateInternalTokenAuthenticationHeaderAsync(
                roles: [SendMeasurementsController.RequiredRole],
                extraClaims: RequiredActorClaims);

        // Act
        var response = await _fixture.WebApiClient.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccessStatusCode, $"Request failed with status code: {response.StatusCode}, response: {responseContent}");
    }
}
