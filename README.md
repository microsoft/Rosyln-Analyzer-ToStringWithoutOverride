# Rosyln-Analyzer-ToStringWithoutOverride
.NET Compiler Platform ("Roslyn") analyzer to disallow calling object.ToString() on types lacking an override.

## Motivation

Imagine we have the following simple [POCO](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object) for representing money:

    struct Money {
        public decimal amount;
        public string currency;
    }

Then we try to print out an instance of it:

    System.Console.WriteLine("I need about {0}", new Money { amount = 3.50m, currency = "$" });

The statement will print `I need about Money`, which is not very useful. This came up a number of times on a team when writing code logging the state of objects for debugging purposes, leading to the creation of this analyzer.

After installing this analyzer the above `WriteLine` call will result in a compile-time error message `Expression of type 'Money' will be implicitly converted to a string, but does not override ToString()`.
