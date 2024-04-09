# Read description in the 'views.dsl' file.

ediDomain = group "EDI" {
    ediDb = container "EDI Database" {
        description "Stores information related to EDI operations"
        technology "SQL Server"
        tags "Data Storage, Microsoft Azure - SQL Database" "Mosaic"
    }
    ediStorageAccount = container "EDI Storage Account" {
        description "Used by EDI azure functions to store state"
        technology "Azure Blob Storage"
        tags "Data Storage, Microsoft Azure - Storage Accounts" "Mosaic"
    }
    edi = container "EDI" {
        description "Backend server providing API for EDI operations"
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps" "Mosaic"

        # Domain relationships
        this -> ediDb "Used by EDI azure functions to store state"
        this -> dh3.sharedServiceBus "Subscribes to integration events"
        this -> ediStorageAccount "Stores state"

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
        this -> edi "Forwards request to backend"

        # Domain-to-domain relationships
        this -> dh3.sharedB2C "Validate credentials" "https" {
        }
    }
    ediB2cWebApi = container "EDI B2C WEB API" {
        description "An Web API for EDI B2C operations"
        technology "Asp.Net Core Web API"
        tags "Microsoft Azure - App Services" "Mosaic"

        # Base model relationships
        dh3User -> this "Requests eg. Aggregated Measure data"

        # Domain relationships
        this -> edi "Forwards request to backend"
    }
}
