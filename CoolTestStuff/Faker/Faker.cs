using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSubstitute;

namespace CoolTestStuff.Faker
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
        private readonly List<SpecifiedInstance> _specifiedDependencies;
        private readonly Lazy<T> _lazyFake;

        /// <summary>
        /// Initialise a default Faker object which will inject fakes into the ctor where it
        /// can and register those fakes for later reference. 
        /// </summary>
        public Faker()
        {
            InjectedFakes = new List<FakedObject>();
            _specifiedDependencies = new List<SpecifiedInstance>();

            _lazyFake = new Lazy<T>(BuildFake);
        }

        /// <summary>
        /// Initialise a Faker[T] with specific dependencies it should use to inject the T with.
        /// </summary>
        /// <param name="specificInstances"></param>
        public Faker(List<SpecifiedInstance> specificInstances)
        {
            InjectedFakes = new List<FakedObject>();
            _specifiedDependencies = specificInstances;

            _lazyFake = new Lazy<T>(BuildFake);
        }


        /// <summary>
        /// A list of all the fakes that were injected into the Fake[T] during its construction.
        /// </summary>
        public List<FakedObject> InjectedFakes { get; }

        /// <summary>
        /// This is the Fake[T] - lazily instantiated.
        /// </summary>
        public T Fake => _lazyFake.Value;

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
            return (TDependency)InjectedFakes
                .First(m => m.TypeThatHasBeenFaked == typeof(TDependency) && m.NameOfFakeInstance == name).Fake;
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
            var ctorParameterInfo = GetMostSpecialisedConstructor()?.GetParameters() ?? Array.Empty<ParameterInfo>();
            foreach (var param in ctorParameterInfo)
            {
                var specifiedInstance = GetSpecifiedInstance(param);
                if (UseSpecifiedInstance(specifiedInstance))
                {
                    constructorValues.Add(specifiedInstance!.Instance);
                    continue;
                }

                if (CanBeFaked(param.ParameterType))
                {
                    constructorValues.Add(CreateFakeFor(param));
                    continue;
                }

                var defaultValue = GetDefault(param.ParameterType);
                if(defaultValue != null)
                    constructorValues.Add(defaultValue);
            }

            return constructorValues.ToArray();
        }

        private object CreateFakeFor(ParameterInfo param)
        {
            var fakeInstance = Substitute.For(new[] { param.ParameterType }, Array.Empty<object>());

            InjectedFakes.Add(
                new FakedObject(param.ParameterType, param.Name, fakeInstance));
            
            return fakeInstance;
        }

        private static bool UseSpecifiedInstance(SpecifiedInstance? specifiedInstance)
        {
            return !specifiedInstance?.Equals(default(SpecifiedInstance)) ?? false;
        }

        private SpecifiedInstance? GetSpecifiedInstance(ParameterInfo paramInfo)
        {
            return _specifiedDependencies
                .FirstOrDefault(
                    o =>
                        paramInfo.ParameterType.IsInstanceOfType(o.Instance) &&
                        paramInfo.Name == (o.Name ?? paramInfo.Name));
        }

        private static bool CanBeFaked(Type dependencyType)
            => dependencyType.IsClass || dependencyType.IsInterface;

        private static ConstructorInfo? GetMostSpecialisedConstructor()
        {
            var allCtors = typeof(T).GetConstructors();

            if (allCtors.Length == 0)
                return null;

            var maxParams = allCtors.Max(ctor => ctor.GetParameters().Length);
            return allCtors.Single(ctor => ctor.GetParameters().Length == maxParams);
        }

        private static object? GetDefault(Type type)
            => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
