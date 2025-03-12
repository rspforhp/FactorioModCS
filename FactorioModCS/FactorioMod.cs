using LuaNt;

namespace FactorioModCS;

public class FactorioMod : IComparable<FactorioMod>, IEquatable<FactorioMod>
{

    public  LuaNtFile Data=new LuaNtFile("data.lua");
   

    public ModInfo Info;
    
    public int CompareTo(FactorioMod? other)
    {
        return String.Compare(Info.name, other.Info.name, StringComparison.Ordinal);
    }
    public bool Equals(FactorioMod? other)
    {
        return String.Equals(Info.name,other.Info.name, StringComparison.Ordinal);
    }

    //TODO: add the dlc features later too maybe
    public FactorioMod(string name, MajorMiddleMinorVersion version, string title, string author,
        string? contact = null, string? homepage = null, string? description = null,
        MajorMinorVersion? factorio_version = null, ModDependency[]? dependencies = null)
    {
        Info = new ModInfo(name, version, title, author, contact, homepage, description, factorio_version, dependencies);
        
    }

}