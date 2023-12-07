namespace EndToEndTesting.Tests.TestData;

public class SynchronousErrorTestData
{
    public Dictionary<string, string> MismatchingSenderIdData()
    {
        return new Dictionary<string, string>
        {
            { "cim:sender_MarketParticipant.mRID", "5790000701413" }
        };
    }
    
    public Dictionary<string, string> SenderRoleTypeNotAuthorized()
    {
        return new Dictionary<string, string>
        {
            { "cim:receiver_MarketParticipant.marketRole.type", "DGL" }
        };
    }
    
    public Dictionary<string, string> MessageIdIsNotUnique()
    {
        return new Dictionary<string, string>
        {
            { "cim:mRID", "B6Qhv7Dls6zdnvgna3cQqXu0PAzFqKco8GLc" }
        };
    }
    
    public Dictionary<string, string> TransactionIdIsNotUnique()
    {
        return new Dictionary<string, string>
        {
            { "cim:mRID", "aX5fNO7st0zVIemSRek4GM1FCSRbQ28PMIZO" }
        };
    }
    
    public Dictionary<string, string> EmptyMessageId()
    {
        return new Dictionary<string, string>
        {
            { "cim:mRID", "" }
        };
    }
    
    public Dictionary<string, string> EmptyTransactionId()
    {
        return new Dictionary<string, string>
        {
            { "cim:mRID", "" }
        };
    }
    
    public Dictionary<string, string> InvalidTransactionId()
    {
        return new Dictionary<string, string>
        {
            { "cim:mRID", "invalidId" }
        };
    }
    
    public Dictionary<string, string> SchemaVersionIsInvalid()
    {
        //TODO: add dynamic handling of xml header creation
        return new Dictionary<string, string>
        {
            { "cim:mRID", "invalidId" }
        };
    }
    
    public Dictionary<string, string> SchemaValidationErrorOnType()
    {
        return new Dictionary<string, string>
        {
            { "cim:businessSector.type", "232" }
        };
    }
    
    public Dictionary<string, string> InvalidReceiverId()
    {
        return new Dictionary<string, string>
        {
            { "cim:receiver_MarketParticipant.mRID", "5790001330553" }
        };
    }
    
    public Dictionary<string, string> InvalidReceiverRole()
    {
        return new Dictionary<string, string>
        {
            { "cim:receiver_MarketParticipant.marketRole.type", "DDZ" }
        };
    }
    
    public Dictionary<string, string> InvalidLengthOfMessageId()
    {
        return new Dictionary<string, string>
        {
            { "cim:mRID", "lasjfejhrtajhfksagjebrtafnnvsgietjafehtaafaertzrshgsyr" }
        };
    }
    
    public Dictionary<string, string> TypeIsNotSupported()
    {
        return new Dictionary<string, string>
        {
            { "cim:type", "E73" }
        };
    }
    
    public Dictionary<string, string> ProcessTypeIsNotSupported()
    {
        return new Dictionary<string, string>
        {
            { "cim:process.processType", "D09" }
        };
    }
    
    public Dictionary<string, string> InvalidBusinessType()
    {
        return new Dictionary<string, string>
        {
            { "cim:businessSector.type", "27" }
        };
    }
    
}
