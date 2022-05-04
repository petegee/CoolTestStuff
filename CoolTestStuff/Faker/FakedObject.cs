using System;

namespace CoolTestStuff.Faker;

public class FakedObject
{
    public FakedObject(Type typeThatHasBeenFaked, string? nameOfFakeInstance, object fake)
    {
        TypeThatHasBeenFaked = typeThatHasBeenFaked;
        NameOfFakeInstance = nameOfFakeInstance;
        Fake = fake;
    }
            
    public Type TypeThatHasBeenFaked { get; }

    public string? NameOfFakeInstance { get; }

    public object Fake { get; }
}