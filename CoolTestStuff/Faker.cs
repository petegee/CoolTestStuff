using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSubstitute;

namespace CoolTestStuff
{
    /// <summary>
    /// A builder which can build a Mock[T]/Fake and inject its .Object instance dependencies with Mocks 
    /// or supplied dependencies via the most specialised constructor. 
    /// It acts as a registry for supplied and/or injected objects for access to them via the GetInjectedMock
    /// methods.
    /// This is based of my https://github.com/petegee/CoolTestStuff project.
    /// </summary>
    /// <typeparam name="T">The type to build a fake of.</typeparam>
    public class Faker<T> where T : class
    {
        private readonly List<KeyValuePair<string, object>> specifiedDependencies;
        private readonly Lazy<T> lazyFake;

        /// <summary>
        /// Initialise a default Faker object which will inject mocks into the ctor where it
        /// can and register those mocks for later reference. It will also default the Mock[T] to
        /// be a partial mock.
        /// </summary>
        public Faker()
        {
            InjectedMocks = new List<RegisteredMock>();
            specifiedDependencies = new List<KeyValuePair<string, object>>();

            lazyFake =
                new Lazy<T>(
                    () => Substitute.ForPartsOf<T>(GetMostSpecialisedConstructorParameterValues()));
                    //() => new Mock<T>(GetMostSpecialisedConstructorParameterValues()) { CallBase = true });
        }

        /// <summary>
        /// Initialise a Faker[T] with specific dependencies it should use to inject the T with, and whether
        /// or not it should support partial mocking.
        /// </summary>
        /// <param name="specificInstances"></param>
        public Faker(List<KeyValuePair<string, object>> specificInstances)
        {
            InjectedMocks = new List<RegisteredMock>();
            specifiedDependencies = specificInstances;

            lazyFake =
                new Lazy<T>(
                    () => Substitute.ForPartsOf<T>(GetMostSpecialisedConstructorParameterValues()));
        }

        /// <summary>
        /// A list of all the mocks that were injected into the Mock[T].Object
        /// </summary>
        public List<RegisteredMock> InjectedMocks { get; set; }

        /// <summary>
        /// This is the Mock[T] - lazily instantiated.
        /// </summary>
        public T Fake => lazyFake.Value;

        ///// <summary>
        ///// This is the Mock[T].Object - eg the faked object T
        ///// </summary>
        //public T Faked => lazyFake.Value.Object;


        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance.
        /// </summary>
        public TDependency GetInjectedMock<TDependency>() where TDependency : class
        {
            return (TDependency)InjectedMocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency)).Mock;
        }

        /// <summary>
        /// Get a Mock which was injected into the SUT (injected via its CTOR) instance naming a parameter.
        /// use only when a SUT has two of the same types injected that are differentiated by parameter name.
        /// NOTE: use GetInjectedMock() with no parameters by default - then there will no magic-strings.
        /// </summary>
        public TDependency GetInjectedMock<TDependency>(string name) where TDependency : class
        {
            return (TDependency)InjectedMocks.First(m => m.TypeThatHasBeenMocked == typeof(TDependency) && m.NameOfMockInstance == name).Mock;
        }

        private object[] GetMostSpecialisedConstructorParameterValues()
        {
            var constructorValues = new List<object>();
            var ctorParameterInfo = GetMostSpecialisedConstructor()?.GetParameters() ?? new ParameterInfo[] { };
            foreach (var param in ctorParameterInfo)
            {
                var specifiedDependency = GetSpecifiedInstance(param);
                if (UseSpecifiedDependency(specifiedDependency))
                {
                    constructorValues.Add(specifiedDependency.Value);
                    continue;
                }

                if (CanBeMocked(param.ParameterType))
                {
                    constructorValues.Add(CreateMockFor(param));
                    continue;
                }

                constructorValues.Add(GetDefault(param.ParameterType));
            }

            return constructorValues.ToArray();
        }

        private object CreateMockFor(ParameterInfo param)
        {
            var mockInstance = Substitute.For(new Type[] { param.ParameterType }, new object[] { });


            InjectedMocks.Add(
                new RegisteredMock
                {
                    Mock = mockInstance,
                    TypeThatHasBeenMocked = param.ParameterType,
                    NameOfMockInstance = param.Name
                });

            return mockInstance;
        }

        private static bool UseSpecifiedDependency(KeyValuePair<string, object> keyValuePair)
        {
            return !keyValuePair.Equals(default(KeyValuePair<string, object>));
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
            var allCtors = typeof(T).GetConstructors();

            if (allCtors.Length == 0)
                return null;

            var maxParams = allCtors.Max(ctor => ctor.GetParameters().Length);
            return allCtors.Single(ctor => ctor.GetParameters().Length == maxParams);
        }

        private static object GetDefault(Type type)
            => type.IsValueType ? Activator.CreateInstance(type) : null;


        public class RegisteredMock
        {
            public Type TypeThatHasBeenMocked { get; set; }
            public string NameOfMockInstance { get; set; }
            public object Mock { get; set; }
        }
    }
}
