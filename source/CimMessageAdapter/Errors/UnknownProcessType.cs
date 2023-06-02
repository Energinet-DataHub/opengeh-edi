namespace CimMessageAdapter.Errors;

public class UnknownProcessType : ValidationError
{
    public UnknownProcessType(string processType)
        : base($"Process type {processType} is not known", "xxx-xxx")
    {
    }
}
