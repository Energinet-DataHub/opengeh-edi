using System;
using Dapper;

namespace Application.SearchMessages;

internal static class QueryBuilder
{
    public static QueryInput BuildFrom(GetMessagesQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var selectStatement =
            "SELECT Id AS MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt FROM dbo.ArchivedMessages";
        var queryParameters = new DynamicParameters();

        if (request.CreationPeriod is null && request.MessageId is not null)
        {
            selectStatement += " WHERE CreatedAt = CreatedAt";
        }

        if (request.CreationPeriod is not null)
        {
            selectStatement += " WHERE CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod";
            queryParameters.Add("StartOfPeriod", request.CreationPeriod.DateToSearchFrom.ToString());
            queryParameters.Add("EndOfPeriod", request.CreationPeriod.DateToSearchTo.ToString());
        }

        if (request.MessageId is not null)
        {
            selectStatement += " AND Id = @MessageId";
            queryParameters.Add("MessageId", request.MessageId.Value.ToString());
        }

        return new QueryInput(selectStatement, queryParameters);
    }
}
