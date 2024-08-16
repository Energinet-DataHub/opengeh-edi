# The 'views.dsl' file is intended as a mean for viewing and validating the model
# in the subsystem repository. It should
#   * Extend the base model and override the 'dh3' software system
#   * Include of the `model.dsl` files from each subsystem repository using an URL
#
# The `model.dsl` file must contain the actual model, and is the piece that must
# be reusable and included in other Structurizr files like `views.dsl` and
# deployment diagram files.

workspace extends https://raw.githubusercontent.com/Energinet-DataHub/opengeh-arch-diagrams/main/docs/diagrams/c4-model/dh-base-model.dsl {

    model {
        #
        # DataHub 3.0 (extends)
        #
        !ref dh3 {

            # IMPORTANT:
            # The order by which models are included is important for how the subsystem-to-subsystem relationships are specified.
            # A subsystem-to-subsystem relationship should be specified in the "client" of a "client->server" dependency, and
            # hence subsystem that doesn't depend on others, should be listed first.

            # Include Wholesale model
            !include https://raw.githubusercontent.com/Energinet-DataHub/opengeh-wholesale/main/docs/diagrams/c4-model/model.dsl

            # Include EDI model
            !include model.dsl

            # Include frontend model - placeholders
            frontendSubsystem = group "Frontend" {
                frontendBff = container "BFF" {
                    description "Backend for Frontend"

                    # Base model relationships
                    dh3User -> this "Requests eg. Aggregated Measure data"

                    # Subsystem-to-Subsystem relationships
                    this -> ediB2cWebApi "Interact using HTTP API"
                }
            }
        }
    }

    views {
        container dh3 "EDI" {
            title "[Container] DataHub 3.0 - EDI (Simplified)"
            include ->ediSubsystem->
            include dh3User
            exclude "element.tag==Intermediate Technology"
            exclude dh3.sharedB2C
        }

        container dh3 "EDIDetailed" {
            title "[Container] DataHub 3.0 - EDI (Detailed with authentication)"
            include ->ediSubsystem->
            include dh3User
            exclude "relationship.tag==Simple View"
        }
    }
}
