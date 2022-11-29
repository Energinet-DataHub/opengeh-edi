namespace Messaging.Application.OutgoingMessages.Peek;

/// <summary>
/// Manage state of the peek message bundling
/// </summary>
public interface IBundlingState
{
    /// <summary>
    ///  Is bundling in progress
    /// </summary>
    /// <param name="key"> </param>
    bool IsBundlingInProgress(string key);

    /// <summary>
    ///  Signal that the bundling is started
    /// </summary>
    /// <param name="key"> </param>
    void BundlingStarted(string key);

    /// <summary>
    ///  Is bundling in completed
    /// </summary>
    /// <param name="key"> </param>
    void BundlingCompleted(string key);
}
