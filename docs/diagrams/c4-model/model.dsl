# Read description in the 'views.dsl' file.

ediSubsystem = group "EDI" {
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
    ediB2b = container "EDI B2B" {
        description "Backend for EDI B2B operations"
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps" "Mosaic"

        # Base model relationships
        actorB2BSystem -> this "Requests" {
            tags "Simple View"
        }

        # Subsystem relationships
        this -> ediDb "Uses"
        this -> dh3.sharedServiceBus "Subscribes to Integration Events and EDI Inbox"
        this -> ediStorageAccount "Stores state"

        # Subsystem-to-Subsystem relationships
        this -> wholesaleRuntimeWarehouse "Retrieves results from"
    }
    ediApi = container "EDI API" {
        description "API Gateway policies for EDI Web API"
        technology "Azure API Management Service"
        tags "Intermediate Technology" "Microsoft Azure - API Management Services" "Mosaic"

        # Base model relationships
        actorB2BSystem -> this "Requests eg. Peek and Dequeue"

        # Subsystem relationships
        this -> ediB2b "Forwards request to backend"

        # Subsystem-to-Subsystem relationships
        this -> dh3.sharedB2C "Validate credentials" "https" {
        }
    }
    ediB2cWebApi = container "EDI B2C WEB API" {
        description "A Web API for EDI B2C operations"
        technology "Asp.Net Core Web API"
        tags "Microsoft Azure - App Services" "Mosaic"

        # Subsystem relationships
        this -> ediDb "Uses"
        this -> ediB2b "Forwards request to backend (using ServiceBus)"
    }
}
