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

using Energinet.DataHub.Wholesale.Edi.UnitTests.Builders;
using Energinet.DataHub.Wholesale.Edi.Validation;
using Energinet.DataHub.Wholesale.Edi.Validation.AggregatedTimeSeriesRequest.Rules;
using Energinet.DataHub.Wholesale.Edi.Validation.Helpers;
using FluentAssertions;
using NodaTime;
using NodaTime.Extensions;
using Xunit;

namespace Energinet.DataHub.Wholesale.Edi.UnitTests.Validators.AggregatedTimeSeriesRequest;

public class PeriodValidatorTests
{
    private static readonly ValidationError _invalidDateFormat = new("Forkert dato format for {PropertyName}, skal være YYYY-MM-DDT22:00:00Z eller YYYY-MM-DDT23:00:00Z / Wrong date format for {PropertyName}, must be YYYY-MM-DDT22:00:00Z or YYYY-MM-DDT23:00:00Z", "D66");
    private static readonly ValidationError _invalidWinterMidnightFormat = new("Forkert dato format for {PropertyName}, skal være YYYY-MM-DDT23:00:00Z / Wrong date format for {PropertyName}, must be YYYY-MM-DDT23:00:00Z", "D66");
    private static readonly ValidationError _invalidSummerMidnightFormat = new("Forkert dato format for {PropertyName}, skal være YYYY-MM-DDT22:00:00Z / Wrong date format for {PropertyName}, must be YYYY-MM-DDT22:00:00Z", "D66");

    private static readonly ValidationError _startDateMustBeLessThen3Years = new(
        "Dato må max være 3 år og 6 måneder tilbage i tid / Can maximum be 3 years and 6 months back in time",
        "E17");

    private static readonly ValidationError _periodIsGreaterThenAllowedPeriodSize = new("Dato må kun være for 1 måned af gangen / Can maximum be for a 1 month period", "E17");
    private static readonly ValidationError _missingStartOrAndEndDate = new("Start og slut dato skal udfyldes / Start and end date must be present in request", "E50");

    private readonly PeriodValidationRule _sut;
    private readonly DateTimeZone? _dateTimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("Europe/Copenhagen");

    private Instant _now;

    public PeriodValidatorTests()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        _now = Instant.FromUtc(now.InUtc().Year, 5, 31, 22, 0, 0);
        _sut = new PeriodValidationRule(new PeriodValidationHelper(_dateTimeZone!, new MockClock(() => _now)));
    }

    [Fact]
    public async Task Validate_WhenRequestIsValid_ReturnsNoValidationErrors()
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WhenEndDateIsUnspecified_ReturnsExpectedValidationError()
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithEndDate(string.Empty)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();
        var error = errors.First();
        error.Message.Should().Be(_missingStartOrAndEndDate.Message);
        error.ErrorCode.Should().Be(_missingStartOrAndEndDate.ErrorCode);
    }

    [Fact]
    public async Task Validate_WhenStartHourIsWrong_ReturnsExpectedValidationError()
    {
        // Arrange
        var notWinterTimeMidnight = Instant.FromUtc(_now.InUtc().Year, 1, 1, 22, 0, 0).ToString();

        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(notWinterTimeMidnight)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();
        var error = errors.First();
        error.ErrorCode.Should().Be(_invalidWinterMidnightFormat.ErrorCode);
        error.Message.Should().Be(_invalidWinterMidnightFormat.WithPropertyName("Start date").Message);
    }

    [Fact]
    public async Task Validate_WhenEndHourIsWrong_ReturnsExpectedValidationError()
    {
        // Arrange
        var notSummerTimeMidnight = Instant.FromUtc(_now.InUtc().Year, 7, 1, 23, 0, 0).ToString();
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithEndDate(notSummerTimeMidnight)
            .WithStartDate(Instant.FromUtc(_now.InUtc().Year, 7, 2, 22, 0, 0).ToString())
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();
        var error = errors.First();
        error.ErrorCode.Should().Be(_invalidSummerMidnightFormat.ErrorCode);
        error.Message.Should().Be(_invalidSummerMidnightFormat.WithPropertyName("End date").Message);
    }

    [Fact]
    public async Task Validate_WhenStartIsUnspecified_ReturnsExpectedValidationError()
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(string.Empty)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();
        var error = errors.First();
        error.ErrorCode.Should().Be(_missingStartOrAndEndDate.ErrorCode);
        error.Message.Should().Be(_missingStartOrAndEndDate.Message);
    }

    [Fact]
    public async Task Validate_WhenStartAndEndDateAreInvalid_ReturnsExpectedValidationErrors()
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate("string.Empty")
            .WithEndDate("string.Empty")
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Count.Should().Be(2);
        errors.Should().Contain(error => error.Message.Contains(_invalidDateFormat.WithPropertyName("Start date").Message)
                                         && error.ErrorCode.Equals(_invalidDateFormat.ErrorCode));
        errors.Should().Contain(error => error.Message.Contains(_invalidDateFormat.WithPropertyName("End date").Message)
                                         && error.ErrorCode.Equals(_invalidDateFormat.ErrorCode));
    }

    [Fact]
    public async Task Validate_WhenPeriodSizeIsGreaterThenAllowed_ReturnsExpectedValidationError()
    {
        // Arrange
        var winterTimeMidnight = Instant.FromUtc(_now.InUtc().Year, 1, 1, 23, 0, 0);

        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(winterTimeMidnight.ToString())
            .WithEndDate(winterTimeMidnight.Plus(Duration.FromDays(32)).ToString())
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();
        var error = errors.First();
        error.ErrorCode.Should().Be(_periodIsGreaterThenAllowedPeriodSize.ErrorCode);
        error.Message.Should().Be(_periodIsGreaterThenAllowedPeriodSize.Message);
    }

    [Fact]
    public async Task Validate_WhenPeriodIsOlderThenAllowed_ReturnsExpectedValidationError()
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(Instant.FromUtc(2018, 1, 1, 23, 0, 0).ToString())
            .WithEndDate(Instant.FromUtc(2018, 1, 1, 23, 0, 0).ToString())
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();
        var error = errors.First();
        error.ErrorCode.Should().Be(_startDateMustBeLessThen3Years.ErrorCode);
        error.Message.Should().Be(_startDateMustBeLessThen3Years.Message);
    }

    [Fact]
    public async Task Validate_WhenPeriodOverlapSummerDaylightSavingTime_ReturnsNoValidationErrors()
    {
        // Arrange
        var winterTime = Instant.FromUtc(2023, 2, 26, 23, 0, 0).ToString();
        var summerTime = Instant.FromUtc(2023, 3, 26, 22, 0, 0).ToString();
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(winterTime)
            .WithEndDate(summerTime)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WhenPeriodOverlapWinterDaylightSavingTime_ReturnsNoValidationErrors()
    {
        // Arrange
        var summerTime = Instant.FromUtc(_now.InUtc().Year, 9, 29, 22, 0, 0).ToString();
        var winterTime = Instant.FromUtc(_now.InUtc().Year, 10, 29, 23, 0, 0).ToString();
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(summerTime)
            .WithEndDate(winterTime)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task
        Validate_WhenPeriodStartIsMoreThan3YearsAnd6MonthsOldAndPeriodNotPartOfCutOffMonth_ReturnsExpectedValidationError()
    {
        // Arrange
        var dateTimeOffset = _now.ToDateTimeOffset().AddYears(-5);

        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(dateTimeOffset.ToInstant().ToString())
            .WithEndDate(dateTimeOffset.AddMonths(1).ToInstant().ToString())
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle().Subject.Should().Be(_startDateMustBeLessThen3Years);
    }

    [Fact]
    public async Task Validate_WhenPeriodStartIsLessThan3YearsAnd6MonthsOld_ReturnNoValidationError()
    {
        // Arrange
        _now = new LocalDateTime(2024, 6, 1, 0, 0, 0)
            .InZoneStrictly(_dateTimeZone!)
            .ToInstant();

        // Using a start date 2 years, 8 months, 14 days, 13 hours, 25 minutes and 37 seconds back in time
        var start = new LocalDateTime(2022, 4, 1, 0, 0, 0)
            .InZoneStrictly(_dateTimeZone!)
            .ToInstant();

        // Using an end date 2 years, 7 months, 14 days, 13 hours, 25 minutes and 37 seconds back in time
        var end = new LocalDateTime(2022, 5, 1, 0, 0, 0)
            .InZoneStrictly(_dateTimeZone!)
            .ToInstant();

        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(start.ToString())
            .WithEndDate(end.ToString())
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task
        Validate_WhenPeriodStartIsMoreThan3And6MonthsBackInTimeButPartOfCutOffMonth_ReturnsNoValidationError()
    {
        // Arrange
        _now = new LocalDateTime(2024, 12, 15, 13, 25, 37)
            .InZoneStrictly(_dateTimeZone!)
            .ToInstant();

        // Using a start date 3 years, 6 months, 14 days, 13 hours, 25 minutes and 37 seconds back in time
        var periodStartDate = new LocalDateTime(2021, 6, 1, 0, 0, 0)
            .InZoneStrictly(_dateTimeZone!)
            .ToInstant();

        // Using an end date 3 years, 5 months, 14 days, 13 hours, 25 minutes and 37 seconds back in time
        var periodEndDate = new LocalDateTime(2021, 7, 1, 0, 0, 0)
            .InZoneStrictly(_dateTimeZone!)
            .ToInstant();

        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithStartDate(periodStartDate.ToString())
            .WithEndDate(periodEndDate.ToString())
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    private sealed class MockClock(Func<Instant> getInstant) : IClock
    {
        private readonly Func<Instant> _getInstant = getInstant;

        public Instant GetCurrentInstant() => _getInstant.Invoke();
    }
}
