using LuaNt;

public static partial class Globals
{
    [IsLuaNt(IsLuaNtAttribute.LuaType.Global)]
    public static TableLib table=>throw ByDesign;
    


}
[IsLuaNt(IsLuaNtAttribute.LuaType.Meta)]
public class TableLib
{
    [IsLuaNt(IsLuaNtAttribute.LuaType.MethodCall)]
    public T deepcopy<T>(T input_table)=>throw Globals.ByDesign;
}