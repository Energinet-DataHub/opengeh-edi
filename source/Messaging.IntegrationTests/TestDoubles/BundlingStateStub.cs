using System.Collections.Generic;
using Messaging.Application.OutgoingMessages.Peek;

namespace Messaging.IntegrationTests.TestDoubles;

internal class BundlingStateStub : IBundlingState
{
    private readonly Dictionary<string, bool> _dictionary = new();
    private bool _alwaysReturnBundlingInProgress;

    public bool IsBundlingInProgress(string key)
    {
        return _alwaysReturnBundlingInProgress || _dictionary.ContainsKey(key);
    }

    public void BundlingStarted(string key)
    {
        _dictionary.Add(key, true);
    }

    public void BundlingCompleted(string key)
    {
        _dictionary.Remove(key);
    }

    public void AlwaysReturnBundlingInProgress()
    {
        _alwaysReturnBundlingInProgress = true;
    }
}
