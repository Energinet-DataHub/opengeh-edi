# The 'views.dsl' file is intended as a mean for viewing and validating the model
# in the domain repository. It should
#   * Extend the base model and override the 'dh3' software system
#   * Include of the `model.dsl` files from each domain repository using an URL
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
            # The order by which models are included is important for how the domain-to-domain relationships are specified.
            # A domain-to-domain relationship should be specified in the "client" of a "client->server" dependency, and
            # hence domains that doesn't depend on others, should be listed first.

            # Include Market Participant model
            !include https://raw.githubusercontent.com/Energinet-DataHub/geh-market-participant/main/docs/diagrams/c4-model/model.dsl

            # Include EDI model
            !include model.dsl

            # Include Wholesale model
            !include https://raw.githubusercontent.com/Energinet-DataHub/opengeh-wholesale/main/docs/diagrams/c4-model/model.dsl

            # Include Frontend model
            !include https://raw.githubusercontent.com/Energinet-DataHub/greenforce-frontend/krmoos/removed-deprecated-containers/docs/diagrams/c4-model/model.dsl
            #!include https://raw.githubusercontent.com/Energinet-DataHub/greenforce-frontend/main/docs/diagrams/c4-model/model.dsl
        }
    }

    views {
        container dh3 "EDI" {
            title "[Container] DataHub 3.0 - EDI (Simplified)"
            include ->ediDomain->
            exclude "element.tag==Intermediate Technology"
            exclude ediDb
            exclude dh3.sharedB2C
        }

        container dh3 "EDIDetailed" {
            title "[Container] DataHub 3.0 - EDI (Detailed with authentication)"
            include ->ediDomain->
            exclude "relationship.tag==Simple View"
        }
    }
}
