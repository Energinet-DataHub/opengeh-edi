using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Messaging.Application.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.Configuration.DataAccess;

namespace Messaging.IntegrationTests.Factories;

public class TestDataBuilder
{
    private readonly TestBase _testBase;
    private readonly List<object> _commands = new();

    public TestDataBuilder(TestBase testBase)
    {
        _testBase = testBase;
    }

    public TestDataBuilder AddActor(Guid actorId, string actorNumber)
    {
        _commands.Add(new CreateActor(actorId.ToString(), Guid.NewGuid().ToString(), actorNumber));
        return this;
    }

    public TestDataBuilder AddMarketEvaluationPoint(Guid marketEvaluationPointId, Guid gridOperatorId, string marketEvaluationPointNumber)
    {
        var marketEvaluationPoint = new MarketEvaluationPoint(
            marketEvaluationPointId,
            marketEvaluationPointNumber);
        marketEvaluationPoint.SetGridOperatorId(gridOperatorId);
        var b2BContext = _testBase.GetService<B2BContext>();
        b2BContext.MarketEvaluationPoints.Add(marketEvaluationPoint);

        return this;
    }

    public async Task BuildAsync()
    {
        foreach (var command in _commands)
        {
            await _testBase.InvokeCommandAsync(command).ConfigureAwait(false);
        }

        var b2BContext = _testBase.GetService<B2BContext>();
        if (b2BContext.ChangeTracker.HasChanges())
            await b2BContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
