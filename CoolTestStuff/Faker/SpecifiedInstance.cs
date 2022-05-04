namespace CoolTestStuff.Faker;

public class SpecifiedInstance
{
    public SpecifiedInstance(object instance, string? instanceName=null)
    {
        Instance = instance;
        Name = instanceName;
    }
    
    public object Instance { get; }
    
    public string? Name { get; }
}