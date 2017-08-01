using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoolTestStuff
{
    /// <summary>
    /// Simple automocking base-class for Unit testing. It contains basic Auto-mocking
    /// of the test Target <typeparamref name="TSut"/> via constructor injection only. It will
    /// not automock public properties.
    /// It also contains access to AutoFixture's builder pattern via the CreateA and CreateAnAutoMocked
    /// methods for building test data or to take control of what objects you compose your SUT with.
    /// </summary>
    /// <typeparam name="TSut"></typeparam>
    public class SystemUnderTest<TSut>
        where TSut : class
    {
        private List<RegisteredMock> systemUnderTestMocks;
        private List<SpecificDependency> specifiedDependencies;
        private Lazy<Mock<TSut>> targetFake;
        private IFixture autoMockingObjectBuilder;
        private IFixture objectBuilder;

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
            objectBuilder = CreateFixture();

            autoMockingObjectBuilder = CreateFixture();
            autoMockingObjectBuilder.Customize(new AutoMoqCustomization());

            specifiedDependencies = new List<SpecificDependency>();

            systemUnderTestMocks = new List<RegisteredMock>();

            // We build the targetFake class as late as possible to all people to use
            // the InjectTargetWith() method to provide custom instances.
            targetFake = new Lazy<Mock<TSut>>(
                () =>
                    // lazily build the SUT/Fake...
                    new Mock<TSut>(GetMostSpecialisedConstructorParameterValues())
                    {
                        CallBase = true
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
        /// Create an instance of T which has been built by Autofixture with all its 
        /// properties recursively built or mocked where it can. Access to these Mocks
        /// are via the GetMockAt() method.
        /// </summary>
        protected T CreateAnAutoMocked<T>()
        {
            return autoMockingObjectBuilder.Create<T>();
        }

        /// <summary>
        /// Create an instance of T which has been built by Autofixture with all its 
        /// properties recursively built - this will have no mocks injected.
        /// </summary>
        protected T CreateA<T>()
        {
            return objectBuilder.Create<T>();
        }

        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance.
        /// </summary>
        protected Mock<TDependency> GetInjectedMock<TDependency>() where TDependency : class
        {
            // in order to get a Mock, then the actual TargetMock needs to be created with all its parameters
            ForceCreationOfLazySystemUnderTest();

            return (Mock<TDependency>)systemUnderTestMocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency)).Mock;
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

            return (Mock<TDependency>)systemUnderTestMocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency) && m.NameOfMockInstance == name).Mock;
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
            specifiedDependencies.Add(new SpecificDependency { Instance = instance });
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
            specifiedDependencies.Add(new SpecificDependency { Instance = instance, ConstructorParameterName = ctorParameterName });
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

        private object[] GetMostSpecialisedConstructorParameterValues()
        {
            var constructorValues = new List<object>();
            foreach (var param in GetMostSpecialisedConstructor().GetParameters())
            {
                var specifiedDependency = GetSpecifiedInstance(param);
                if (specifiedDependency != null)
                {
                    constructorValues.Add(specifiedDependency.Instance);
                    continue;
                }

                if (CanBeMocked(param.ParameterType))
                {
                    var mockInstance = CreateMock(param.ParameterType);
                    systemUnderTestMocks.Add(
                        new RegisteredMock
                        {
                            Mock = mockInstance,
                            TypeThatHasBeenMocked = param.ParameterType,
                            NameOfMockInstance = param.Name
                        });

                    constructorValues.Add(mockInstance.Object);
                    continue;
                }

                constructorValues.Add(GetDefault(param.ParameterType));
            }

            return constructorValues.ToArray();
        }

        private SpecificDependency GetSpecifiedInstance(ParameterInfo paramInfo)
        {
            return specifiedDependencies
                .FirstOrDefault(
                    o =>
                        paramInfo.ParameterType.IsInstanceOfType(o.Instance) &&
                        paramInfo.Name == (o.ConstructorParameterName ?? paramInfo.Name));
        }

        private static bool CanBeMocked(Type dependencyType) 
            => dependencyType.IsClass || dependencyType.IsInterface;

        private static ConstructorInfo GetMostSpecialisedConstructor()
        {
            var allCtors = typeof(TSut).GetConstructors();
            var maxParams = allCtors.Max(ctor => ctor.GetParameters().Length);
            return allCtors.Single(ctor => ctor.GetParameters().Length == maxParams);
        }

        private static Mock CreateMock(Type dependencyType)
            => (Mock)typeof(Mock<>).MakeGenericType(dependencyType)
                    .GetConstructor(new Type[0])
                    ?.Invoke(new object[0]);

        private static object GetDefault(Type type)
            => type.IsValueType ? Activator.CreateInstance(type) : null;

        private IFixture CreateFixture()
        {
            var fixture = new Fixture();
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => autoMockingObjectBuilder?.Behaviors?.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            return fixture;
        }

        private class RegisteredMock
        {
            public Type TypeThatHasBeenMocked { get; set; }
            public string NameOfMockInstance { get; set; }
            public Mock Mock { get; set; }
        }

        private class SpecificDependency
        {
            public string ConstructorParameterName { get; set; }
            public object Instance { get; set; }
        }
    }
}
