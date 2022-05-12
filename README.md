# CoolTestStuff

<a href="https://www.buymeacoffee.com/peteg" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

## My Auto-mocking Container and a Fake builder

This is my crusty automocking container/base test class which:
* Automocks your class under test, saving you from ceremony cruft code and concentrate on the intent of the test.
* Supports partial mocking of the SUT. 
* Supports injection of supplied instances as required, else it will mock all dependencies automatically
* Lazy instantiation of the SUT.
* Has a Faker class that you can use to build Fake instances, which you can inject into your SUT for n-level deep System-Under-Test style tests. 
* Uses NSubstitute under the hood.
* Can be used with any testing framework (nUnit, xUnit etc)

## How to use
You can choose to directly use either the:
 - `SystemUnderTest<T>` class as the basis for your unit test. Simply inherit your test class from this.
 - `Faker<T>` class if you have your own test base class and/or if you want a more low-level experience.

### SystemUnderTest
1. Create a class to contain your unit tests and derive it from `SystemUnderTest<TSut>` where `TSut` is your class/system being tested.
2. The auto-mocking container will create an instance of your `TSut` class using the widest constructor and injecting in default NSubstitute substitutes for each injected dependency. Your new `TSut` class instance is available in your test method as `Target`. 
3. Create a test method calling your method to test using the `Target` reference.
4. You can get access to the injected substitutes by using one of the `GetInjectedFake()` methods to setup and/or verify invocations.

```csharp
interface IBar
{
   void DoStuff();
}

class Foo
{
    IBar _bar;
    
    Foo(IBar bar)
    {
       _bar = bar;
    }
    
    void DoSomething()
    {
         _bar.DoStuff();
    }
}


class Test : SystemUnderTest<Foo>
{
   [Fact]
   void TestStuff()
   {       
       // Act - test your method.
       Target.DoSomething();
       
       // Assert - verify it got called.
       GetInjectedFake<IBar>().Received().DoStuff();
   }
}

```

5. You can alternatively provide fake/mock instances to the automocking container to use instead of it constructing its own default substitute dependencies.

### Faker
You can use the Faker class directly if you have your own unit test base class and/or want more control over how and when the system under test class is built and managed.

```csharp
interface IBar
{
   void DoStuff();
}

class Foo
{
    IBar _bar;
    
    Foo(IBar bar)
    {
       _bar = bar;
    }
    
    void DoSomething()
    {
         _bar.DoStuff();
    }
}


class Test
{
   [Fact]
   void TestStuff()
   {       
       var systemUnderTest = new Faker<Foo>();
       
       // Act - test your method.
       systemUnderTest.DoSomething();
       
       // Assert - verify it got called.
       systemUnderTest.GetInjectedFake<IBar>().Received().DoStuff();
   }
}

```
## Examples
Best place to start is with the provided unit tests.



