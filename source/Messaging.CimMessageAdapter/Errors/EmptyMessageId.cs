using JetBrains.Annotations;

namespace Messaging.CimMessageAdapter.Errors;

public class EmptyMessageId : ValidationError
{
    public EmptyMessageId()
        : base($"Message id cannot be empty", "B2B-003")
    {
    }
}
