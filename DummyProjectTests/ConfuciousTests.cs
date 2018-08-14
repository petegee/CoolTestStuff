using CoolTestStuff;
using DummyProject;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DummyProjectTests
{
    public class ConfuciousTests : SystemUnderTest<Confucious>
    {
        [Fact]
        public void BasicUsageExample()
        {
            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("(real GetQuote())");
        }

        [Fact]
        public void BasicUsageExampleWithSubstituteAssertion()
        {
            var result = Target.ImpartWiseWordsOfWisdom();

            Target.PhilosophicalQuoteGenerator.SaySomething().Received();
            Target.MovieQuoteGenerator.SaySomething().Received();
        }


        [Fact]
        public void PartialMockWillCallMockedGetQuoteMethod()
        {
            Target.When(t => t.GetTheQuoutes()).DoNotCallBase();

            Target.GetTheQuoutes().Returns("Mocked to all hell!");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("Mocked to all hell!");
            result.Should().NotContain("(real GetQuote())");
        }

        [Fact]
        public void UseDifferentSpecificVersion()
        {
            // Magic strings are ugly - dont use me unless you really have too...
            // consider making properties public so you can use the non-magic string version
            // of InjectTargetWith()
            InjectTargetWith(new CowQuouteGenerator(), "movieQuoteGenerator");
            InjectTargetWith(new DinosaurQuouteGenerator(), "philosophicalQuoteGenerator");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("RROOOOOOAAAAARRRR!");
            result.Should().Contain("MOOOOOO!");
        }

        [Fact]
        public void UseSingleSpecificVersion()
        {
            InjectTargetWith(new CowQuouteGenerator(), "movieQuoteGenerator");
            Target.PhilosophicalQuoteGenerator.SaySomething()
                .Returns("You are just a brain in a vat...");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("MOOOOOO!");
            result.Should().Contain("You are just a brain in a vat...");
        }

        [Fact]
        public void UseDefaultSpecificVersionForAll()
        {
            InjectTargetWith(new CowQuouteGenerator());

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Match("*MOOOOOO!*MOOOOOO!*");
        }

        [Fact]
        public void NLevelDeepMockTests()
        {
            Target.PhilosophicalQuoteGenerator.SaySomething()
                .Returns("You are just a brain in a vat...");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("You are just a brain in a vat...");
        }

        [Fact]
        public void GetInjectedMock()
        {
            GetInjectedFake<IQuoteGenerator>("philosophicalQuoteGenerator")
                .SaySomething()
                .Returns("you're just a brain in a vat!");

            GetInjectedFake<IQuoteGenerator>("movieQuoteGenerator")
                .SaySomething()
                .Returns("Do you feel lucky today?");

            // Act
            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("Do you feel lucky today?");
            result.Should().Contain("you're just a brain in a vat!");
        }
        
        [Fact]
        public void YoureBetterOffAccessingInjectedFakesViaPublicPropertiesInsteadOfGetInjectedMockWithNamedParameters()
        {
            // Look mum, no magic strings!
            Target.MovieQuoteGenerator.SaySomething()
                .Returns("you're just a brain in a vat!");

            Target.PhilosophicalQuoteGenerator.SaySomething()
                .Returns("Do you feel lucky today?");

            // Act
            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("Do you feel lucky today?");
            result.Should().Contain("you're just a brain in a vat!");
        }

        [Fact]
        public void LetsInjectARealMovieQuoteGeneratorWithFakedDependenciesIntoOurSutWhichWeCanSetup()
        {
            // Arrange
            var imdbFaker = new Faker<IImdb>();
            imdbFaker.Fake.GetTopMovieQuote()
                .Returns("You cant handle the truth!");

            // build a real MovieQuoteGenerator instance and manually inject it...
            var realMovieQuoteGeneratorWithFakeImdb = new MovieQuoteGenerator(imdbFaker.Fake) as IQuoteGenerator;

            // and pass that instance to be injected into out SUT.
            InjectTargetWith(realMovieQuoteGeneratorWithFakeImdb);

            // Act
            var result = Target.ImpartWiseWordsOfWisdom();

            // Assert
            result.Should().Contain("You cant handle the truth!");
        }

        [Fact]
        public void LetsInjectAFakeMovieQuoteGeneratorIntoOurSutWhichWeCanSetup()
        {
            // Arrange - build a fake MovieQuoteGenerator and inject our SUT with its .Object
            var quoteGeneratorFaker = new Faker<MovieQuoteGenerator>();
            InjectTargetWith(quoteGeneratorFaker.Fake);

            // now setup the fake/mock directly via the faker.
            quoteGeneratorFaker.GetInjectedFake<IImdb>()
                .GetTopMovieQuote()
                .Returns("You cant handle the truth!");

            // Act
            var result = Target.ImpartWiseWordsOfWisdom();

            // Assert - are we having fun yet?
            result.Should().Contain("You cant handle the truth!");
        }

        public class DinosaurQuouteGenerator : IQuoteGenerator
        {
            public string SaySomething() => "RROOOOOOAAAAARRRR!";
        }
        public class CowQuouteGenerator : IQuoteGenerator
        {
            public string SaySomething() => "MOOOOOO!";
        }
    }
    

}
