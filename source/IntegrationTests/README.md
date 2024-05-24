# EDI integration test project

As of right now, the following naming convention is [automatically enforced](./Behaviours/MetaTests.cs) for tests
located in the `Behaviours` folder.
But not for tests located outside of this folder, it is therefore up to the individual to enfore the descriped
guidelines/convention

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

That is. Most, if not all, of the variable are defined as the first thing.

Hereafter executing the method(s) one wants to test. Save the return value in a variable with a descriptive name,
if this is not doable use `actual` as the variable name.

Then the assertions begin.
When asserting it is preferred to use `FluentAssertion` and make use of `AssertionScope()`, as in the example above.

## Naming the tests

The structure described in the [section above](#tests-structure) give rise to the following naming
schema `Given_XXX_When_YYY_Then_ZZZ`.
Where:

`XXX` states the important variables (Related to the `//Arrange` section of the test).

`YYY` is the action or a description of the action that is tested (Related to the `//Act` section of the test).

`ZZZ` is a description of the expected consequence of the action stated in `YYY` (Related to the `//Assert` section of the test).

### Alternative test naming

If one from whatever reason should follow another naming convention than stated in the [naming section](#naming-the-tests) one should add the `[ExcludeFromNameConventionCheck]` attribute to the test. As is done in the following example:

```cs
    [ExcludeFromNameConventionCheck]
    public static void ThisDoesNotSatisfyTheNamingConventionFact()
    {
        
    }
```

Which serves two purposes.

- Firstly: That the test is excluded for the [naming convention check](./Behaviours/MetaTests.cs).
- Secondly: It makes it clear that one purposely did not follow the naming convention. And hopefully has a good reason for doing so.  

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
