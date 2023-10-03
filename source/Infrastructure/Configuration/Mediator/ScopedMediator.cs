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

using System;
using MediatR;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.Mediator;

public class ScopedMediator : IDisposable
{
    private readonly IDisposable _scope;
    private readonly IMediator _mediator;
    private bool _isDisposed;

    public ScopedMediator(IDisposable scope, IMediator mediator)
    {
        _scope = scope;
        _mediator = mediator;
    }

    public IMediator Mediator
    {
        get
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(_mediator), "Scoped mediator is disposed");

            return _mediator;
        }
    }

    public void Dispose()
    {
        // https://learn.microsoft.com/da-dk/dotnet/fundamentals/code-analysis/quality-rules/ca1063
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _scope.Dispose();
        }

        _isDisposed = true;
    }
}
