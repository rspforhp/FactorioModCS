using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using LuaNt.LuaFunctions;

namespace FactorioModCS;

public record ModInfo
{
    public readonly string name;
    public readonly  MajorMiddleMinorVersion version;
    public readonly  string title;
    public readonly  string author;
    public readonly  string? contact;
    public readonly  string? homepage = null;
    public readonly  string? description = null;
    public readonly  MajorMinorVersion? factorio_version = null;
    public readonly  ModDependency[]? dependencies = null;

    public override string ToString()
    {
         StringBuilder b=new StringBuilder();
         b.Append(this.TryMakeLua());
         return b.ToString();
    }

    public static bool IsNameValid(string n)
    {
        return n.All(c=>char.IsLetterOrDigit(c) || c=='-' || c=='_');
    }
    public ModInfo(string name, MajorMiddleMinorVersion version, string title, string author, string? contact, string? homepage, string? description, MajorMinorVersion? factorioVersion, ModDependency[]? dependencies)
    {
        if (!IsNameValid(name)) throw new Exception($"Invalid string {name}");
        if (title.Length > 100) throw new Exception("Title can't be longer than 100 characters!");

        this.name = name;
        this.version = version;
        this.title = title;
        this.author = author;
        this.contact = contact;
        this.homepage = homepage;
        this.description = description;
        factorio_version = factorioVersion;
        this.dependencies = dependencies;
    }
}
