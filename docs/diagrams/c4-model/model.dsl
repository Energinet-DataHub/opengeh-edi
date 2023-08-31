# Read description in the 'views.dsl' file.

ediDomain = group "EDI" {
    ediDb = container "EDI Database" {
        description "Stores information related to EDI operations"
        technology "SQL Server"
        tags "Data Storage, Microsoft Azure - SQL Database" "Mosaic"
    }
    edi = container "EDI" {
        description "Backend server providing API for EDI operations"
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps" "Mosaic"

        ediPeekComponent = component "Peek component" {
            description "Handles peek requests from actors"
            technology "Http Trigger"
            tags "Microsoft Azure - Function Apps" "Mosaic"

            # Domain relationships
            this -> ediDb "Stores messages and business transactions" "EF Core, Dapper"
        }
        ediDequeueComponent = component "Dequeue component" {
            description "Handles dequeue requests from actors"
            technology "Http Trigger"
            tags "Microsoft Azure - Function Apps" "Mosaic"

            # Domain relationships
            this -> ediDb "Deletes messages that have been peeked" "EF Core"
        }
        ediTimeSeriesRequester = component "TimeSeries request component" {
            description "Fetches time series data from relevant domain"
            technology "<?> Trigger"
            tags "Microsoft Azure - Function Apps" "Mosaic"

            # Domain relationships
            this -> ediDb "Writes time series data to database" "EF Core"
        }
        ediTimeSeriesListener = component "TimeSeries listener" {
            description "Listens for integration events indicating time series data is ready"
            technology "Service Bus Trigger"
            tags "Microsoft Azure - Function Apps" "Mosaic"

            # Base model relationships
            this -> dh3.sharedServiceBus "Subscribes to integration events"

            # Domain relationships
            this -> ediTimeSeriesRequester "Triggers requester to fetch time series data"
        }

        # Base model relationships
        actorB2BSystem -> this "Requests" {
            tags "Simple View"
        }
    }
    ediApi = container "EDI API" {
        description "API Gateway policies for EDI Web API"
        technology "Azure API Management Service"
        tags "Intermediate Technology" "Microsoft Azure - API Management Services" "Mosaic"

        # Base model relationships
        actorB2BSystem -> this "Requests eg. Peek and Dequeue"

        # Domain relationships
        this -> ediPeekComponent "Requests"
        this -> ediDequeueComponent "Dequeue messages"

        # Domain-to-domain relationships
        this -> dh3.sharedB2C "Validate credentials" "https" {
        }
    }
}
