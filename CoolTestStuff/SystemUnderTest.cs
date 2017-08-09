using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace CoolTestStuff
{
    /// <summary>
    /// Simple automocking base-class for Unit testing. It contains basic Auto-mocking
    /// of the test Target <typeparamref name="TSut"/> via constructor injection only. It will
    /// not automock public properties.
    /// </summary>
    /// <typeparam name="TSut"></typeparam>
    public class SystemUnderTest<TSut>
        where TSut : class
    {
        private List<KeyValuePair<string, object>> specifiedDependencies;
        private Lazy<Mock<TSut>> targetFake;
        private Faker<TSut> targetFaker;

        /// <summary>
        /// The SUT test-target fake. Use this to set up partial-mock
        /// expectations on your SUT test-target.
        /// </summary>
        protected Mock<TSut> TargetFake => targetFake.Value;

        /// <summary>
        /// Access to the actual SUT test-target 
        /// </summary>
        protected TSut Target => TargetFake.Object;

        [OneTimeSetUp]
        protected void PerRunSetup()
        {
            DoPerRunSetUp();
        }

        [OneTimeTearDown]
        protected void PerRunTeardown()
        {
            DoPerRunTeardown();
        }

        [SetUp]
        protected void PerTestSetup()
        {
            specifiedDependencies = new List<KeyValuePair<string, object>>();

            // We build the targetFake class as late as possible to all people to use
            // the InjectTargetWith() method to provide custom instances.
            targetFake = new Lazy<Mock<TSut>>(
                () =>
                {
                    // lazily build the SUT/Fake...
                    targetFaker = new Faker<TSut>(specifiedDependencies, callBase: true);
                    return targetFaker.Fake;
                });


            DoPerTestSetUp();
        }

        [TearDown]
        protected void PerTestTeardown()
        {
            DoPerTestTearDown();
        }

        /// <summary>
        /// Override in your test class as required to hook into nUnit execution path.
        /// </summary>
        protected virtual void DoPerRunSetUp() { }

        /// <summary>
        /// Override in your test class as required to hook into nUnit execution path.
        /// </summary>
        protected virtual void DoPerRunTeardown() { }

        /// <summary>
        /// Override in your test class as required to hook into nUnit execution path.
        /// </summary>
        protected virtual void DoPerTestSetUp() { }

        /// <summary>
        /// Override in your test class as required to hook into nUnit execution path.
        /// </summary>
        protected virtual void DoPerTestTearDown() { }

        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance.
        /// </summary>
        protected Mock<TDependency> GetInjectedMock<TDependency>() where TDependency : class
        {
            // in order to get a Mock, then the actual TargetMock needs to be created with all its parameters
            ForceCreationOfLazySystemUnderTest();

            return targetFaker.GetInjectedMock<TDependency>();
        }

        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance naming a parameter.
        /// use only when a SUT has two of the same types injected that are differentiated by parameter name.
        /// NOTE: use GetInjectedMock() with no parameters by default - then there will no magic-strings.
        /// </summary>
        protected Mock<TDependency> GetInjectedMock<TDependency>(string name) where TDependency : class
        {
            // in order to get a Mock, then the actual TargetMock needs to be created with all its parameters
            ForceCreationOfLazySystemUnderTest();

            return targetFaker.GetInjectedMock<TDependency>(name);
        }

        /// <summary>
        /// Simple wrapper around Moqs Mock.Get() at a specific property.
        /// Use this to get access to Mocks which the builders may have created when/if you used
        /// CreateAnAutoMocked() or CreateA() methods. 
        /// </summary>
        protected Mock<T> GetMockAt<T>(T mockReference) where T : class
            => Mock.Get(mockReference);

        /// <summary>
        /// Ensure this instance of an object is used when building the SUT.
        /// This method will not do anything once the Lazy Target property gets 
        /// evaluated. Ensure you use this before anything else in your tests.
        /// </summary>
        protected void InjectTargetWith<T>(T instance) where T : class
        {
            specifiedDependencies.Add(new KeyValuePair<string, object>(null, instance));
        }

        /// <summary>
        /// Ensure this instance of an object is used when building the SUT naming 
        /// the parameter.
        /// This method will not do anything once the Lazy Target property gets 
        /// evaluated. Ensure you use this before anything else in your tests.
        /// NOTE: use InjectTargetWith() with no parameters by default - then there will no magic-strings.
        /// </summary>
        protected void InjectTargetWith<T>(T instance, string ctorParameterName) where T : class
        {
            specifiedDependencies.Add(new KeyValuePair<string, object>(ctorParameterName, instance));
        }

        /// <summary>
        /// Clears any pre-registered specified instances to use.
        /// </summary>
        protected void ClearAllRegisteredInstances()
        {
            specifiedDependencies.Clear();
        }

        private void ForceCreationOfLazySystemUnderTest()
        {
            // this forces the creation of the Lazy SUT to happen now.
            var iexistOnlyToForceTheCreationOfTheSystemUnderTest = targetFake.Value;
        }
    }
}
