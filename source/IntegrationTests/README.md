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

The `Behaviours` test have a special kind of folder structure.
Since these kind of tests are supposed to test the behavior of the system given some external input.
Which gives rise to a different kind of class naming, namely that the classes are named as the following: `Given{ExternalInput}Tests`.
eg:

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

By doing so, one does not need to specify the external input in the specific test method. Which allow the writer to
specify the important variables in the test, as descripbed in [naming section](#naming-the-tests).

If one finds it unpleasent to have a class named `Given{ExternalInput}Tests` with a method named `Given{ImportantVairalbes}_When_YYY_Then_ZZZ`.
Then it is possible the use the prefix `AndGiven_` instead of `Given_`. It is even possible to leave it out and start
with `When_`.

### Propper integration tests

Unlike the `Behaiviours` tests these tests are more isolated and test a single responsibility. Eg test of
integrationEventHandlers, repositories and the like.

Since the responsibility is different, so is the folder structure.
In fact, there isn't any good structure for the time being :I.

But one should try to keep the test for a single module to it's own folder.
eg:

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

Here `Trancastions` are supposed to test the `process` module and the `integrationEvent` module. Which should be moved
to a more specialized test project at some point

Note that the naming convention mentioned in the [naming section](#naming-the-tests) is not automatically enforced but
it is prefer that one tries to follow it anyway.
