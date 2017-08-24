using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CoolTestStuff
{
    /// <summary>
    /// Simple auto-faking/mocking base-class for Unit testing. It contains basic Auto-mocking
    /// of the test Target <typeparamref name="TSut"/> via constructor injection only. It will
    /// not automock public properties.
    /// </summary>
    /// <typeparam name="TSut"></typeparam>
    public class SystemUnderTest<TSut>
        where TSut : class
    {
        private List<KeyValuePair<string, object>> specifiedDependencies;
        private Lazy<TSut> targetFake;
        private Faker<TSut> targetFaker;

        /// <summary>
        /// Access to the actual SUT test-target 
        /// </summary>
        protected TSut Target => targetFake.Value;

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
            targetFake = new Lazy<TSut>(
                () =>
                {
                    // lazily build the SUT/Fake...
                    targetFaker = new Faker<TSut>(specifiedDependencies);
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
        /// Get a Fake which was injected into the SUT (injected via its CTOR) instance.
        /// </summary>
        protected TDependency GetInjectedFake<TDependency>() where TDependency : class
        {
            ForceCreationOfLazySystemUnderTest();

            return targetFaker.GetInjectedFake<TDependency>();
        }

        /// <summary>
        /// Get a Fake which was injected into the SUT (injected via its CTOR) instance naming a parameter.
        /// use only when a SUT has two of the same types injected that are differentiated by parameter name.
        /// NOTE: use GetInjectedFake() with no parameters by default - then there will no magic-strings.
        /// </summary>
        protected TDependency GetInjectedFake<TDependency>(string name) where TDependency : class
        {
            ForceCreationOfLazySystemUnderTest();

            return targetFaker.GetInjectedFake<TDependency>(name);
        }

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
