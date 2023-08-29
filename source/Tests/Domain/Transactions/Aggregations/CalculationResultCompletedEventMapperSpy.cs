using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Infrastructure.Configuration.IntegrationEvents;
using Infrastructure.Transactions.Aggregations;
using MediatR;

namespace Tests.Domain.Transactions.Aggregations;

public class CalculationResultCompletedEventMapperSpy : CalculationResultCompletedEventMapper
{
    public CalculationResultCompletedEventMapperSpy(IGridAreaLookup gridAreaLookup)
        : base(gridAreaLookup)
    {
    }

    public static void MapProcessTypeSpy(ProcessType processType)
    {
        MapProcessType(processType);
    }
}
