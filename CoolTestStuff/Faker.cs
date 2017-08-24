using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSubstitute;

namespace CoolTestStuff
{
    /// <summary>
    /// A builder which can build a Mock/Fake and inject its dependencies with Fakes 
    /// or supplied dependencies via the most specialised constructor. 
    /// It acts as a registry for supplied and/or injected objects for access to them via the GetInjectedFake
    /// methods.
    /// This is based of my https://github.com/petegee/CoolTestStuff project.
    /// </summary>
    /// <typeparam name="T">The type to build a fake of.</typeparam>
    public class Faker<T> where T : class
    {
        private readonly List<KeyValuePair<string, object>> specifiedDependencies;
        private readonly Lazy<T> lazyFake;

        /// <summary>
        /// Initialise a default Faker object which will inject fakes into the ctor where it
        /// can and register those fakes for later reference. 
        /// </summary>
        public Faker()
        {
            InjectedFakes = new List<RegisteredFake>();
            specifiedDependencies = new List<KeyValuePair<string, object>>();

            lazyFake = new Lazy<T>(BuildFake);
        }

        /// <summary>
        /// Initialise a Faker[T] with specific dependencies it should use to inject the T with.
        /// </summary>
        /// <param name="specificInstances"></param>
        public Faker(List<KeyValuePair<string, object>> specificInstances)
        {
            InjectedFakes = new List<RegisteredFake>();
            specifiedDependencies = specificInstances;

            lazyFake = new Lazy<T>(BuildFake);
        }


        /// <summary>
        /// A list of all the fakes that were injected into the Fake[T] during its construction.
        /// </summary>
        public List<RegisteredFake> InjectedFakes { get; set; }

        /// <summary>
        /// This is the Fake[T] - lazily instantiated.
        /// </summary>
        public T Fake => lazyFake.Value;

        /// <summary>
        /// Get a Fake which was injected into the SUT (injected via its CTOR) instance.
        /// </summary>
        public TDependency GetInjectedFake<TDependency>() where TDependency : class
        {
            return (TDependency)InjectedFakes.First(m => m.TypeThatHasBeenFaked == typeof(TDependency)).Fake;
        }

        /// <summary>
        /// Get a Fake which was injected into the SUT (injected via its CTOR) instance naming a parameter.
        /// use only when a SUT has two of the same types injected that are differentiated by parameter name.
        /// NOTE: use GetInjectedFake() with no parameters by default - then there will no magic-strings.
        /// </summary>
        public TDependency GetInjectedFake<TDependency>(string name) where TDependency : class
        {
            return (TDependency)InjectedFakes.First(m => m.TypeThatHasBeenFaked == typeof(TDependency) && m.NameOfFakeInstance == name).Fake;
        }

        private T BuildFake()
        {
            return typeof(T).IsInterface
                ? Substitute.For<T>(GetMostSpecialisedConstructorParameterValues())
                : Substitute.ForPartsOf<T>(GetMostSpecialisedConstructorParameterValues());
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

                if (CanBeFaked(param.ParameterType))
                {
                    constructorValues.Add(CreateFakeFor(param));
                    continue;
                }

                constructorValues.Add(GetDefault(param.ParameterType));
            }

            return constructorValues.ToArray();
        }

        private object CreateFakeFor(ParameterInfo param)
        {
            var fakeInstance = Substitute.For(new Type[] { param.ParameterType }, new object[] { });

            InjectedFakes.Add(
                new RegisteredFake
                {
                    Fake = fakeInstance,
                    TypeThatHasBeenFaked = param.ParameterType,
                    NameOfFakeInstance = param.Name
                });

            return fakeInstance;
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

        private static bool CanBeFaked(Type dependencyType)
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


        public class RegisteredFake
        {
            public Type TypeThatHasBeenFaked { get; set; }

            public string NameOfFakeInstance { get; set; }

            public object Fake { get; set; }
        }
    }
}
