# EDI integration test project

As of right now, the following naming convention is [automatically enforced](./Behaviours/MetaTests.cs) for tests
located in the `Behaviours` folder.
It is not enforced for tests located outside of this particular folder, and it is thus the responsibility of the
developer to enforce and adhere to the following guidelines.

## Tests structure

For the EDI subsystem we have the following structure for most of our tests.

```cs
public static void Given_ImportantVariable_When_AlteringImportantVariable_Then_ImportantVariableHasChanged()
{
    // Arrange
    var importantVariable = "I'm one of the important variables for this test";

    // Act
    var actual = IDoSomethingToTheImportantVariable(importantVariable)

    // Assert
    using var scope = new AssertionScope();
    actual.ImportantVarialbeIsChanged.Should().Be(true)
}
```

That is, we are adhering to the well-established "Arrange, Act, and Assert"-pattern (AAA, 3As, ...).
There exists a plethora of information and guidelines regarding AAA, but for the uninitiated, it can be summarised as:

- In the arrange section, you bring the system under test (SUT) and its dependencies to a desired state
- In the act section, you call methods on the SUT, pass the prepared dependencies, and capture the output value (if any)
- In the assert section, you verify the outcome. The outcome may be represented by a return value, the final state of
  the SUT and its collaborators, or the methods the SUT called on those collaborators

We have a few EDI-specific conventions, in addition to the established AAA conventions:

If no suitable name can be given to the return value, actual or result are good fallback values
When doing assertions, we much prefer the usage of `FluentAssertion` and the utilisation of `AssertionScope`.
This is to ensure the assertions are readable, gives good error messages, and that a given test return all errors
immediately.

## Naming the tests

The structure described in the [section above](#tests-structure) give rise to the following naming
schema `Given_XXX_When_YYY_Then_ZZZ`.
Where:

`XXX` states the important set-up (Related to the `//Arrange` section of the test).

`YYY` is the action or a description of the action that is tested (Related to the `//Act` section of the test).

`ZZZ` is a description of the expected consequence of the action stated in `YYY` (Related to the `//Assert` section of the test).

### Alternative test naming

If, for whatever (good) reason, one desire to deviate from the [naming section](#naming-the-tests), one should utilise
the [ExcludeFromNameConventionCheck] attribute.
This should however not be done lightly, and adding the justification in a comment to the test is recommended.
Eg:

```cs
    // Some sort of justification in the form of a comment, summary or the like.
    [ExcludeFromNameConventionCheck]
    public static void ThisDoesNotSatisfyTheNamingConventionFact()
    {
        
    }
```

## Folder structure

### Behaviours test

Our Behaviours tests are additionally subject to further restrictions (or guidelines if you prefer)
with related to folder structure and file naming.
As the primary goal of these tests are to validate the behaviour of the (sub-)system given some
external stimulus (e.g. an integration event or request),
this external stimulus has to be part of the name of the given test class.
All tests for the same external stimulus are therefore located in the same test file.
In other words: all behaviour tests must be named as follows: Given{ExternalInput}Tests.

Furthermore, the tests must be group according to the kind of stimulus we are testing: integration events,incoming requests, etc...
This is achieved by folders IntegrationEvents, IncomingRequests, etc., directly in the root of Behaviours.
The combination of these two rules are illustrated below with an example.

```text
├── Behaviours
│   ├── IntegrationEvents
│   │   ├── GivenAmountPerChargeResultProducedV1ReceivedTests.cs
|   |   ├── GivenMonthlyAmountPerChargeResultProducedV1ReceivedTests.cs
│   └── IncomingRequests
│       ├── GivenAggregatedMeasureDataRequestTests.cs
|       └── GivenAggregatedMeasureDataRequestWithDelegationTests.cs
├── DocumentAsserters
├── EventBuilders
```

By doing so, one does not have to "waste" space on specifying the external input in each and every single test method's [Given-part](#naming-the-tests),
but can instead focus on the uniqueness of each given test.

Similarly, if one finds it unpleasant to have a class named `Given{ExternalInput}Tests` with a method named `Given{ImportantSetup}_When_YYY_Then_ZZZ`,
it is possible the use the prefix `AndGiven_` instead of `Given_`.
In the cases where there are no additional setup, it is possible to start with `When_` directly.
However, we advise caution in this case, as it is rare for a test to have no setup.

### Proper integration tests

Unlike the `Behaiviours` tests these tests are more isolated and test a single responsibility. Eg test of
integrationEventHandlers, repositories and the like.

Since the responsibility is different, so is the folder structure.
In fact, there isn't any good structure for the time being :I.

That said, one should try to keep the test for a single module to it's own folder.
This way they can be extracted to their own modules eventually, with relative ease.

```text
IntegrationTests
├── Application
|   ├── ArchivedMessages
|   ├── IncomingMessages
|   └── Transactions
|       ├── AggregatedMeasureData
|       ├── Aggregations
|       ├── WholesaleCalculations
|       └── WholesaleServices
├── B2BApi
├── Infrastructure
├── DocumentAsserters
├── EventBuilders
```

In the example above, the tests in the Transactions folder are testing both the Process and IntegrationEvent module.
Ideally, if possible, the tests that are limited to a single module should be moved to that modules respective folder (or actual test module).
For the cross-module integration tests, no well-established guideline currently exists.

Do note, that while the [naming convention](#naming-the-tests) is not enforced here, it is advisable to adhere to it as much as possible.
This ensures a consistent naming across the solution.
