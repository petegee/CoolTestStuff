using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;

namespace CoolTestStuff
{
    public class Faker<TSut> where TSut : class
    {
        private readonly List<KeyValuePair<string, object>> specifiedDependencies;
        private readonly Lazy<Mock<TSut>> lazyFake;

        public Faker()
        {
            InjectedMocks = new List<RegisteredMock>();
            specifiedDependencies = new List<KeyValuePair<string, object>>();

            lazyFake =
                new Lazy<Mock<TSut>>(
                    () => new Mock<TSut>(GetMostSpecialisedConstructorParameterValues()) { CallBase = true });
        }

        public Faker(List<KeyValuePair<string, object>> specificInstances, bool callBase = true)
        {
            InjectedMocks = new List<RegisteredMock>();
            specifiedDependencies = specificInstances;

            lazyFake =
                new Lazy<Mock<TSut>>(
                    () => new Mock<TSut>(GetMostSpecialisedConstructorParameterValues()) { CallBase = callBase });
        }

        public List<RegisteredMock> InjectedMocks { get; set; }

        public Mock<TSut> Fake => lazyFake.Value;

        public TSut Faked => lazyFake.Value.Object;


        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance.
        /// </summary>
        public Mock<TDependency> GetInjectedMock<TDependency>() where TDependency : class
        {
            return (Mock<TDependency>)InjectedMocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency)).Mock;
        }

        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance naming a parameter.
        /// use only when a SUT has two of the same types injected that are differentiated by parameter name.
        /// NOTE: use GetInjectedMock() with no parameters by default - then there will no magic-strings.
        /// </summary>
        public Mock<TDependency> GetInjectedMock<TDependency>(string name) where TDependency : class
        {
            return (Mock<TDependency>)InjectedMocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency) && m.NameOfMockInstance == name).Mock;
        }

        private object[] GetMostSpecialisedConstructorParameterValues()
        {
            var constructorValues = new List<object>();
            foreach (var param in GetMostSpecialisedConstructor().GetParameters())
            {
                var specifiedDependency = GetSpecifiedInstance(param);
                if (!specifiedDependency.Equals(default(KeyValuePair<string, object>)))
                {
                    constructorValues.Add(specifiedDependency.Value);
                    continue;
                }

                if (CanBeMocked(param.ParameterType))
                {
                    var mockInstance = CreateMock(param.ParameterType);
                    InjectedMocks.Add(
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

        private KeyValuePair<string, object> GetSpecifiedInstance(ParameterInfo paramInfo)
        {
            return specifiedDependencies
                .FirstOrDefault(
                    o =>
                        paramInfo.ParameterType.IsInstanceOfType(o.Value) &&
                        paramInfo.Name == (o.Key ?? paramInfo.Name));
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


        public class RegisteredMock
        {
            public Type TypeThatHasBeenMocked { get; set; }
            public string NameOfMockInstance { get; set; }
            public Mock Mock { get; set; }
        }
    }
}
