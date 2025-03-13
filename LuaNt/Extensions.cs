using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.Utils;

namespace LuaNt.LuaFunctions;

public static class Extensions
{
    public static MethodBase ToNormal(this MethodReference def)
    {
        return def.Resolve().ToNormal();
    }

    public static MethodBase ToNormal(this MethodDefinition def)
    {
        var declaringType = def.DeclaringType.ToNormal();
        var p = def.Parameters.ToList().FindAll(a => a.ParameterType.GetType() != typeof(GenericParameter))
            .Select(a =>
            {
                var ar = a.ParameterType.Resolve().ToNormal();
                if (a.ParameterType.IsArray) ar = ar.MakeArrayType();
                return ar;
            }).ToArray();
        MethodBase method = null;
        var metname = def.Name;
        if (metname==".ctor")
        {
        
                    if (p.Length > 0)
                        method =AccessTools.Constructor(declaringType,  p);
                    else method = AccessTools.Constructor(declaringType);

        }
        else
        {
            if (p.Length > 0)
                method = AccessTools.Method(declaringType, metname, p);
            else method = AccessTools.Method(declaringType, metname);
        }
    
        if (method == null) throw new MethodAccessException();
        return method;
    }

    public static PropertyInfo? Property(this MethodBase def)
    {
        if (!def.Name.StartsWith("get_") && !def.Name.StartsWith("set_")) return null;
        var typeName = def.DeclaringType.FullName;
        var type = AccessTools.TypeByName(typeName);
        var method = AccessTools.Property(type, def.Name.Replace("set_", "").Replace("get_", ""));
        if (method == null) throw new MethodAccessException();
        return method;
    }

    public static Type ToNormal(this TypeDefinition def)
    {
        var typeName = def.FullName;
        var type = AccessTools.TypeByName(typeName);
        if (type == null) throw new TypeLoadException();
        return type;
    }

    public static string ToSnakeCase(this string o) =>
        Regex.Replace(o, @"(\w)([A-Z])", "$1_$2").ToLower();

    public static string TryMakeLua(this object o)
    {
        StringBuilder b = new StringBuilder();
        b.Append("{\n");
        foreach (var field in o.GetType().GetRuntimeFields())
        {
            if (field.IsPublic)
            {
                var v = field.GetValue(o);
                bool isNullable = field.GetCustomAttribute<NullableAttribute>() != null ||
                                  field.FieldType == typeof(string);
                if (!isNullable|| v!=null)
                {
                    if (v is Array ar)
                    {
                        b.Append($"\"{field.Name.ToSnakeCase()}\": [");
                        foreach (var e in ar)
                        {
                            b.Append($"\"{e.ToString()}\",\n");
                        }

                        if (ar.Length > 0) b.Remove(b.Length - 2, 2);
                        b.Append("],\n");
                    }
                    else b.Append($"\"{field.Name.ToSnakeCase()}\": \"{v.ToString()}\",\n");
                }
            }
        }

        b.Remove(b.Length - 2, 2);
        b.Append("\n}");
        return b.ToString();
    }
}