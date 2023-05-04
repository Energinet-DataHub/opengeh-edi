using System.Collections.Generic;
using Dapper;

namespace Application.SearchMessages;

internal static class QueryBuilder
{
    public static QueryInput BuildFrom(GetMessagesQuery request)
    {
        var queryParameters = new DynamicParameters();
        var statement = new List<string>();

        if (request.CreationPeriod is not null)
        {
            statement.Add("CreatedAt BETWEEN @StartOfPeriod AND @EndOfPeriod");
            queryParameters.Add("StartOfPeriod", request.CreationPeriod.DateToSearchFrom.ToString());
            queryParameters.Add("EndOfPeriod", request.CreationPeriod.DateToSearchTo.ToString());
        }

        if (request.MessageId is not null)
        {
            statement.Add("Id=@MessageId");
            queryParameters.Add("MessageId", request.MessageId.Value.ToString());
        }

        return new QueryInput(BuildStatement(statement), queryParameters);
    }

    private static string BuildStatement(IReadOnlyCollection<string> dic)
    {
        var statement = "SELECT Id AS MessageId, DocumentType, SenderNumber, ReceiverNumber, CreatedAt FROM dbo.ArchivedMessages";
        if (dic.Count > 0)
        {
            statement += " WHERE " + string.Join(" AND ", dic);
        }

        return statement;
    }
}
