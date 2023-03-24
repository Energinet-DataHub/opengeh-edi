workspace extends https://raw.githubusercontent.com/Energinet-DataHub/opengeh-arch-diagrams/main/source/datahub3-model/model.dsl {

    model {
        !ref dh3 {
            wholesale = container "Wholesale

            edi = group "EDI" {
                peekComponent = container "Peek component." "Handles peek requests from actors" "C#, Azure function" {
                    extSoftSystem -> this "Peek messages"
                    tags "Microsoft Azure - Function Apps"
                }
                dequeueComponent = container "Dequeue component" "Handles dequeue requests from actors" "C#, Azure function" {
                    extSoftSystem -> this "Dequeue messages"
                    tags "Microsoft Azure - Function Apps"
                }
                timeSeriesListener = container "TimeSeries listener" "Listens for integration events indicating time series data is ready" "C#, Azure function" {
                    tags "Microsoft Azure - Function Apps"
                }
                timeSeriesRequester = container "TimeSeries request component" "Fetches timeseries data from relevant domain" "C#, Azure function" {
                    timeSeriesListener -> this "Triggers requester to fetch time series data"
                    this -> wholeSale "Fetch time series data"
                    tags "Microsoft Azure - Function Apps"
                }
                database = container "Database" "Stores information related to business transactions and outgoing messages" "SQL server database" {
                    peekComponent -> this "Reads / generates messages"
                    dequeueComponent -> this "Deletes messages that have been peeked"
                    timeSeriesRequester -> this "Writes time series data to database"
                    tags "Microsoft Azure - SQL Database" "Data Storage"
                }
            }
            performanceTest = group "PerformanceTest" {
                testApi = container "API component" "Exposes performance test helper functions" "ASP.NET web API" {
                }
                simulationComponent = container "Simulation" "Simulates actors" "Grafana K6" {
                    this -> testApi "Fecth actor numbers and tokens"
                    this -> peekComponent "Peek messages"
                    this -> dequeueComponent "Dequeue messages"
                }
            }
        }
        eventQueue = element "SB Queue" "Azure service bus" {
                timeSeriesListener -> this "Handles events indicating time series data is available"
                tags "Microsoft Azure - Service Bus"
        }
    }

    views {
        container dh3 {
            include *
            autolayout lr
        }

        container dh3 "EDI" {
            include "EDI"
            autolayout lr
        }

        container dh3 "Performancetest" {
            include "PerformanceTest"
            autolayout lr
        }
}