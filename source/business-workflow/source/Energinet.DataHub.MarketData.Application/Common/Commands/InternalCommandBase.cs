using System;

namespace Energinet.DataHub.MarketData.Application.Common.Commands
{
    public abstract class InternalCommandBase : ICommand
    {
        protected InternalCommandBase(Guid id)
        {
            Id = id;
        }

        protected InternalCommandBase()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }
    }
}
