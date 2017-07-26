﻿using CoolTestStuff;
using DummyProject;
using FluentAssertions;
using NUnit.Framework;

namespace DummyProjectTests
{
    [TestFixture]
    public class ConfuciousTests : SystemUnderTest<Confucious>
    {
        [Test]
        public void PartialMockWillCallMockedGetQuoteMethod()
        {
            TargetFake.Setup(f => f.GetTheQuoutes())
                .Returns("Mocked to all hell!");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("Mocked to all hell!");
            result.Should().NotContain("(real GetQuote())");
        }

        [Test]
        public void ShouldCallBaseMethods()
        {
            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("(real GetQuote())");
        }

        [Test]
        public void UseDifferentSpecificVersion()
        {
            // Magic strings are ugly - dont use me unless you really have too...
            // consider making properties public so you can use the non-magic string version
            // of InjectTargetWith() and then use GetMockAt().
            InjectTargetWith(new CowQuouteGenerator(), "movieQuoteGenerator");
            InjectTargetWith(new DinosaurQuouteGenerator(), "philosophicalQuoteGenerator");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("RROOOOOOAAAAARRRR!");
            result.Should().Contain("MOOOOOO!");
        }

        [Test]
        public void UseSingleSpecificVersion()
        {
            InjectTargetWith(new CowQuouteGenerator(), "movieQuoteGenerator");
            GetMockAt(Target.PhilosophicalQuoteGenerator)
                .Setup(pg => pg.SaySomething())
                .Returns("You are just a brain in a vat...");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("MOOOOOO!");
            result.Should().Contain("You are just a brain in a vat...");
        }

        [Test]
        public void UseDefaultSpecificVersionForAll()
        {
            InjectTargetWith(new CowQuouteGenerator());

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Match("*MOOOOOO!*MOOOOOO!*");
        }

        [Test]
        public void GetMockAt()
        {
            GetMockAt(Target.PhilosophicalQuoteGenerator)
                .Setup(pg => pg.SaySomething())
                .Returns("You are just a brain in a vat...");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("You are just a brain in a vat...");
        }

        [Test]
        public void GetInjectedMock()
        {
            GetInjectedMock<IQuoteGenerator>("philosophicalQuoteGenerator")
                .Setup(pg => pg.SaySomething())
                .Returns("you're just a brain in a vat!");

            GetInjectedMock<IQuoteGenerator>("movieQuoteGenerator")
                .Setup(pg => pg.SaySomething())
                .Returns("Do you feel lucky today?");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("Do you feel lucky today?");
            result.Should().Contain("you're just a brain in a vat!");
        }

        [Test]
        public void YoureBetterOffUsingGetMockAtInsteadOfGetInjectedMockWithNamedParameters()
        {
            // Look mum, no magic strings!
            GetMockAt(Target.MovieQuoteGenerator)
                .Setup(pg => pg.SaySomething())
                .Returns("you're just a brain in a vat!");

            GetMockAt(Target.PhilosophicalQuoteGenerator)
                .Setup(pg => pg.SaySomething())
                .Returns("Do you feel lucky today?");

            var result = Target.ImpartWiseWordsOfWisdom();

            result.Should().Contain("Do you feel lucky today?");
            result.Should().Contain("you're just a brain in a vat!");
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
