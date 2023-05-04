# Read description in the '<domain name>-views.dsl' file.

ediDomain = group "EDI" {
    ediDb = container "EDI Database" {
        description "Stores information related to business transactions and outgoing messages"
        technology "SQL Database Schema"
        tags "Data Storage, Microsoft Azure - SQL Database"
    }
    ediApiApp = container "EDI Web API" {
        description "<add description>"
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps"

        ediPeekComponent = component "Peek component" {
            description "Handles peek requests from actors"
            technology "Http Trigger"
            tags "Microsoft Azure - Function Apps"

            # Domain relationships
            this -> ediDb "Reads / generates messages" "EF Core"
        }
        ediDequeueComponent = component "Dequeue component" {
            description "Handles dequeue requests from actors"
            technology "Http Trigger"
            tags "Microsoft Azure - Function Apps"

            # Domain relationships
            this -> ediDb "Deletes messages that have been peeked" "EF Core"
        }
        ediTimeSeriesRequester = component "TimeSeries request component" {
            description "Fetches time series data from relevant domain"
            technology "<?> Trigger"
            tags "Microsoft Azure - Function Apps"

            # Domain relationships
            this -> ediDb "Writes time series data to database" "EF Core"
        }
        ediTimeSeriesListener = component "TimeSeries listener" {
            description "Listens for integration events indicating time series data is ready"
            technology "Service Bus Trigger"
            tags "Microsoft Azure - Function Apps"

            # Base model relationships
            this -> dh3.sharedServiceBus "Handles events indicating time series data is available"

            # Domain relationships
            this -> ediTimeSeriesRequester "Triggers requester to fetch time series data"
        }

        # Base model relationships
        actorB2BSystem -> this "Peek/Dequeue messages" {
            tags "Simple View"
        }
    }
    ediApi = container "EDI API" {
        description "API Gateway to EDI Web API"
        technology "Azure API Management Service"
        tags "Intermediate Technology" "Microsoft Azure - API Management Services"

        # Base model relationships
        actorB2BSystem -> this "Peek/Dequeue messages"

        # Domain relationships
        this -> ediPeekComponent "Peek messages"
        this -> ediDequeueComponent "Dequeue messages"

        # Domain-to-domain relationships
        this -> commonB2C "Validate OAuth token" "https" {
            tags "OAuth"
        }
    }
}
