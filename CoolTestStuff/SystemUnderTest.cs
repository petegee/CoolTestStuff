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
    public class SystemUnderTest<TSut>
        where TSut : class
    {
        private List<RegisteredMock> mocks;
        private List<SpecificDependency> specifiedDependencies;
        private Lazy<Mock<TSut>> targetFake;

        protected Mock<TSut> TargetFake => targetFake.Value;
        protected TSut Target => TargetFake.Object;
        protected IFixture AutoFixture { get; set; }

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
            AutoFixture = new Fixture()
                .Customize(new AutoMoqCustomization());

            AutoFixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => AutoFixture.Behaviors.Remove(b));

            AutoFixture.Behaviors.Add(new OmitOnRecursionBehavior());

            specifiedDependencies = new List<SpecificDependency>();

            mocks = new List<RegisteredMock>();

            targetFake = new Lazy<Mock<TSut>>(
                () =>
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

        protected T CreateA<T>()
        {
            return AutoFixture.Create<T>();
        }

        protected virtual void DoPerRunSetUp() { }

        protected virtual void DoPerRunTeardown() { }

        protected virtual void DoPerTestSetUp() { }

        protected virtual void DoPerTestTearDown() { }

        protected Mock<TDependency> GetInjectedMock<TDependency>() where TDependency : class
        {
            // in order to get a Mock, then the actual TargetMock needs to be created with all its parameters
            ForceCreationOfLazySystemUnderTest();

            return (Mock<TDependency>)mocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency)).Mock;
        }

        protected Mock<TDependency> GetInjectedMock<TDependency>(string name) where TDependency : class
        {
            // in order to get a Mock, then the actual TargetMock needs to be created with all its parameters
            ForceCreationOfLazySystemUnderTest();

            return (Mock<TDependency>)mocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency) && m.NameOfMockInstance == name).Mock;
        }

        protected Mock<T> GetMockAt<T>(T mockReference) where T : class
            => Mock.Get(mockReference);

        protected void InjectTargetWith<T>(T instance) where T : class
        {
            specifiedDependencies.Add(new SpecificDependency { Instance = instance });
        }

        protected void InjectTargetWith<T>(T instance, string ctorParameterName) where T : class
        {
            specifiedDependencies.Add(new SpecificDependency { Instance = instance, ConstructorParameterName = ctorParameterName });
        }

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
                    mocks.Add(
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
                        paramInfo.ParameterType.IsAssignableFrom(o.Instance.GetType()) && 
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
