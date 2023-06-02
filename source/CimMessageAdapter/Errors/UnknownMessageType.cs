namespace CimMessageAdapter.Errors;

public class UnknownMessageType : ValidationError
{
    public UnknownMessageType(string type)
        : base($"Message type {type} is not known", "xxx-xxx")
    {
    }
}
