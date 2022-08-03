using JetBrains.Annotations;

namespace Messaging.CimMessageAdapter.Errors;

public class EmptyTransactionId : ValidationError
{
    public EmptyTransactionId([NotNull] string message)
        : base($"Transaction id may not be empty", "B2B-005")
    {
    }
}
