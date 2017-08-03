using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoolTestStuff
{
    /// <summary>
    /// Builds a Fake version of 
    /// </summary>
    public class FakeObjectBuilder
    {
        public FakeObjectBuilder()
        {
            InjectedMocks = new List<RegisteredMock>();
        }

        public List<RegisteredMock> InjectedMocks { get; set; }

        public Mock<TSut> BuildFake<TSut>(bool callBaseImplementations=true, List<KeyValuePair<string, object>> specifiedDependencies = null) where TSut : class
        {
            return new Mock<TSut>(GetMostSpecialisedConstructorParameterValues<TSut>(specifiedDependencies))
            {
                CallBase = callBaseImplementations
            };
        }

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

        private object[] GetMostSpecialisedConstructorParameterValues<TSut>(List<KeyValuePair<string, object>> specifiedDependencies) where TSut : class
        {
            var constructorValues = new List<object>();
            foreach (var param in GetMostSpecialisedConstructor<TSut>().GetParameters())
            {
                var specifiedDependency = GetSpecifiedInstance(specifiedDependencies, param);
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

        private KeyValuePair<string, object> GetSpecifiedInstance(List<KeyValuePair<string, object>> specifiedDependencies, ParameterInfo paramInfo)
        {
            return specifiedDependencies
                .FirstOrDefault(
                    o =>
                        paramInfo.ParameterType.IsInstanceOfType(o.Value) &&
                        paramInfo.Name == (o.Key ?? paramInfo.Name));
        }

        private static bool CanBeMocked(Type dependencyType)
            => dependencyType.IsClass || dependencyType.IsInterface;

        private static ConstructorInfo GetMostSpecialisedConstructor<TSut>() where TSut : class
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
