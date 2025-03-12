namespace LuaNt;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public sealed class IsLuaNtAttribute : Attribute
{
    public readonly LuaType _what;

    public enum LuaType
    {
        NONE,
        Global,
        Meta,
        MethodCall,
    }
    public IsLuaNtAttribute(LuaType what)
    {
        _what = what;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class HideLuaNtAttribute : Attribute
{
    public HideLuaNtAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class ReplaceTireWithUnderscoreAttribute : Attribute
{
    public ReplaceTireWithUnderscoreAttribute()
    {
    }
}
