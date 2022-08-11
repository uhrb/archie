namespace archie.Commands;


public abstract class OptionBaseAttribute: Attribute {
    public string[] Aliases { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class OptionAttribute<T> : OptionBaseAttribute
{
    public T? Default { get; set; }
}