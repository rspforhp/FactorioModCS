using Factorio;
using LuaNt;

namespace Factorio
{
    public abstract partial class Data
    {
        public void extend(AnyPrototype[] prototypes) => throw Globals.ByDesign;
    }
}

/// <summary>
/// Those aren't readable via csharp
/// </summary>
public static partial class Globals
{
    public static NotImplementedException ByDesign=>new NotImplementedException();
    //Data stage
    [IsLuaNt(IsLuaNtAttribute.LuaType.Global)]
    public static Data data => throw ByDesign;
    [IsLuaNt(IsLuaNtAttribute.LuaType.Global)]
    public static Mods mods => throw ByDesign;
    [IsLuaNt(IsLuaNtAttribute.LuaType.Global)]
    public static Settings settings => throw ByDesign;
    [IsLuaNt(IsLuaNtAttribute.LuaType.Global)]
    public static FeatureFlags feature_flags => throw ByDesign;
    //
    
}