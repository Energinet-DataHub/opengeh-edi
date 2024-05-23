# EDI integration test project

As of right now, the following naming convention is automatically enforced for tests located in the `Behaviours` folder.
But not for test located outside this folder, it is therefore up to the individual to enfore the descriped guidelines

## Tests structure

For the EDI subsystem we have the following structure for most of our tests.

```cs
// Arrange
var importantVariable = "I'm one of the important variable for the test";

// Act
var actual = IDoSomethingToTheImportantVariable(importantVariable)

// Assert
using var scope = new AssertionScope();
actual.ImportantVarialbeIsChanged.Should().Be(true)
```

That is. Most, if not all, of the variable are defined as the first thing.

Hereafter executing the method(s) one wants to test. Save the return value in a variable with a descriptive name,
if this is not durable use `actual` as the variable name.

Hereafter the assertions begin.
When asserting it is preferred to use `FluentAssertion` and make use of `AssertionScope()`, as in the example above.

## Naming the tests

The structure described in the section: [tests structure](#tests-structure) give rise to the following naming schema `Given_XXX_When_YYY_Then_ZZZ`.
Where:

`XXX` states the important variables (Related to the `//Arrange` section).

`YYY` is the action or a description of the action that is tested (Related to the `//Act` section).

`ZZZ` is a description of the expected values of the action stated in `YYY` (Related to the `//Assert` section).

### Alternative test naming

If one from whatever reason should follow another naming convention than stated in the [naming section](#naming-the-tests) one should add the `[ExcludeFromNameConventionCheck]` attribute to the test. As is done in the following example:

```cs
    [ExcludeFromNameConventionCheck]
    public static void ThisDoesNotSatisfyTheNamingConventionFact()
    {
        ...
    }
```

Which serves two purpose.

- Firstly: That the test is excluded for the naming convention check.
- Secondly: It makes it clear that one purposely did not follow the naming convention. And hopefully has a good reason for doing so.  
