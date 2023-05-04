using Dapper;

namespace Application.SearchMessages;

public record QueryInput(string SqlStatement, DynamicParameters Parameters);
