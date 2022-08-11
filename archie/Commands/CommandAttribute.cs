namespace archie.Commands;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class CommandAttribute : Attribute
{
    public string Name {get;set;}
    public string Description {get;set;}
}