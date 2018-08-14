# CoolTestStuff
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

### Basics
1. Create a class to contain your unit tests and derive it from `SystemUnderTest<TSut>` where `TSut` is your class/system being tested.
2. The auto-mocking container will create an instance of your `TSut` class using the widest constructor and injecting in default NSubstitute substitutes for each injected dependency. Your new `TSut` class instance is available in your test method as `Target`. 
3. Create a test method calling your method to test using the `Target` reference.
4. You can get access to the injected substitutes by using one of the `GetInjectedFake()` methods to setup and/or verify invocations.

```
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
  
### Advanced
5. You can alternatively provide fake/mock instances to the automocking container to use instead of it constructing its own default substitute dependencies.

## Examples
Best place to start is with the provided unit tests.



