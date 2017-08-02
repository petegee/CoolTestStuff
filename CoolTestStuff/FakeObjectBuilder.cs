using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoolTestStuff
{
    public class FakeObjectBuilder<TSut> where TSut : class
    {
        private List<KeyValuePair<string, object>> specifiedDependencies;

        public FakeObjectBuilder()
        {
            specifiedDependencies = new List<KeyValuePair<string, object>>();
        }

        public FakeObjectBuilder(List<KeyValuePair<string, object>> specificInstances)
        {
            specifiedDependencies = specificInstances;
        }

        public Mock<TSut> BuildFake()
        {
            return new Mock<TSut>(GetMostSpecialisedConstructorParameterValues());
        }

        private object[] GetMostSpecialisedConstructorParameterValues()
        {
            var constructorValues = new List<object>();
            foreach (var param in GetMostSpecialisedConstructor().GetParameters())
            {
                var specifiedDependency = GetSpecifiedInstance(param);
                if (specifiedDependency != null)
                {
                    constructorValues.Add(specifiedDependency.Value);
                    continue;
                }

                if (CanBeMocked(param.ParameterType))
                {
                    var mockInstance = CreateMock(param.ParameterType);
                    //systemUnderTestMocks.Add(
                    //    new RegisteredMock
                    //    {
                    //        Mock = mockInstance,
                    //        TypeThatHasBeenMocked = param.ParameterType,
                    //        NameOfMockInstance = param.Name
                    //    });

                    constructorValues.Add(mockInstance.Object);
                    continue;
                }

                constructorValues.Add(GetDefault(param.ParameterType));
            }

            return constructorValues.ToArray();
        }

        private KeyValuePair<string, object>? GetSpecifiedInstance(ParameterInfo paramInfo)
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
    }
}
