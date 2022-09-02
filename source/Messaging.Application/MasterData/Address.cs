using System;

namespace Messaging.Application.MasterData;

public record Address(
    string StreetName,
    string StreetCode,
    string PostCode,
    string City,
    string CountryCode,
    string CitySubDivision,
    string Floor,
    string Room,
    string BuildingNumber,
    int MunicipalityCode,
    bool IsActualAddress,
    Guid GeoInfoReference,
    string LocationDescription);
