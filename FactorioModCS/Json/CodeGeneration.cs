using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneOf;

namespace FactorioModCS.Json;

public static class CodeGeneration
{
    public static string ToJson<T>(this T value, Formatting formatting = Formatting.None)
    {
        if (value == null) return "null";
        try
        {
            return JsonConvert.SerializeObject(value, formatting);
        }
        catch
        {
            return "Exception";
        }
    }

    public static T FromJson<T>(this string value)
    {
        if (value == "null") return default(T);
        try
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
        catch (Exception e)
        {
            throw e;
            return default(T);
        }
    }


    private static int I;

    private static string GetType(PrototypeApi.Property.Value val, string name, StringBuilder sb,
        out object defaultValue)
    {
        string type = null;
        defaultValue = null;
        if (val.directValue != null)
        {
            type = val.directValue;
        }
        else if (val.complex_type == "literal")
        {
            defaultValue = val.value.ToString();
            type = val.value.GetType().Name;
        }
        else if (val.complex_type == "union")
        {
            if (string.IsNullOrEmpty(name)) name = $"_anonymous_{++I}";
            val.options = val.options.DistinctBy(a => GetType(a, "", null, out _)).ToArray();
            if (val.options.Length == 1)
            {
                type = GetType(val.options[0], "", sb, out defaultValue);
            }
            else
            {
                if (val.options.Any(a => a.complex_type == "literal"))
                {
                    sb.AppendLine($"public enum {name}_union_enum{{");
                    foreach (var vale in val.options.ToList().FindAll(a => a.complex_type == "literal"))
                    {
                        var strVale = vale.value.ToString();
                        if (strVale.Contains("-"))
                        {
                            sb.AppendLine("[ReplaceTireWithUnderscore]");
                            strVale = strVale.Replace("-", "_");
                        }

                        sb.AppendLine($"{strVale},");
                    }

                    sb.AppendLine("}");
                    type = $"{name}_union_enum";
                }

                if (val.options.Any(a => a.complex_type != "literal"))
                {
                    if (name == "RandomRange")
                    {
                        Console.WriteLine("debug");
                    }
                    sb.AppendLine($"public class {name}_union : OneOfBase<");
                    List<string> types = null;
                    if (val.options.Any(a => a.complex_type == "literal"))
                    {
                        types = val.options.ToList().FindAll(a => a.complex_type != "literal")
                            .Select(a => GetType(a, "", sb, out _)).ToList();
                        types.Add($"{name}_union_enum");
                    }
                    else types = val.options.Select(a => GetType(a, "", sb, out _)).ToList();

                    foreach (var vale in types)
                    {
                        sb.Append($"{vale},");
                    }

                    sb.Remove(sb.Length - 1, 1);
                    sb.AppendLine("> {");

                    // public Status(Idle idle) : base(0, idle) {}
                    // public static implicit operator Status(Idle value) => value == null? null : new Status(value);
                    for (int i = 0; i < types.Count; i++)
                    {
                        var vale = types[i];
                        sb.AppendLine($"public {name}_union({vale} d) : base(d) {{}}");
                        sb.AppendLine(
                            $"public static implicit operator {name}_union({vale} d) => d == null? null : new {name}_union(d);");
                    }

                    sb.AppendLine("}");
                    type = $"{name}_union";
                }
            }
        }
        else if (val.complex_type == "array")
        {
            if (val.value is JObject j)
            {
                PrototypeApi.Property.Value v = j.ToObject<PrototypeApi.Property.Value>();
                if (v == null) throw new Exception("Not implemented!");
                type = $"{GetType(v, "", sb, out _)}[]";
            }
            else type = $"{val.value}[]";
        }
        else if (val.complex_type == "dictionary")
        {
            var keyType = GetType(val.key, "", sb, out _);
            if (val.value is JObject j)
            {
                PrototypeApi.Property.Value v = j.ToObject<PrototypeApi.Property.Value>();
                if (v == null) throw new Exception("Not implemented!");
                type = $"Dictionary<{keyType},{(GetType(v, "", sb, out _))}>";
            }
            else
                type = $"Dictionary<{keyType},{val.value}>";
        }
        else if (val.complex_type == "tuple")
        {
            type = $"Tuple<";
            foreach (var vale in val.values)
            {
                type += $"{GetType(vale, "", sb, out _)},";
            }

            type = type[..^1];
            type += ">";
        }
        else if (val.complex_type == "type")
        {
            if(val.value is string)
             type = val.value.ToString();
            else if (val.value is JObject j)
            {
                PrototypeApi.Property.Value v = j.ToObject<PrototypeApi.Property.Value>();
                if (v == null) throw new Exception("Not implemented!");
                type = GetType(v, "", sb, out _);
            }
            else throw new NotImplementedException();
        }

        if (type == null) throw new Exception("Not implemented");
        if (type is "string" or "String" && defaultValue != null)
        {
            if (!defaultValue.ToString().StartsWith("\""))
                defaultValue = $"\"{defaultValue}\"";
        }

        return type;
    }

    private static string GetType(PrototypeApi.Property property, StringBuilder sb, out object defaultValue)
    {
        string type = null;
        type = GetType(property.type, property.name, sb, out defaultValue);
        if (type == null) throw new Exception("Not implemented");
        return type;
    }

    private static PrototypeApi.Prototype DeepestParent(PrototypeApi.Prototype p)
    {
        if (string.IsNullOrEmpty(p.parent)) return p;
        var parent = Types[p.parent];
        return DeepestParent(parent);
    }

    private static void ProcessPrototype(PrototypeApi.Prototype prot, StringBuilder gSb)
    {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(prot.description))
        {
            sb.Append($"/// <summary>\n///{prot.description.Replace("\n", "\n///")}\n/// </summary>\n");
        }

        if (prot.deprecated) sb.AppendLine("[Obsolete]");
        if (prot.type == null || string.IsNullOrEmpty(prot.type.directValue))
        {
            if (prot.name.Contains("-"))
            {
                sb.AppendLine("[ReplaceTireWithUnderscore]");
                prot.name = prot.name.Replace("-", "_");
            }


            if (prot.values == null)
            {
                string add = "";
                if (prot.type != null && prot.type.complex_type == "union" && prot.type.options != null &&
                    prot.type.options.All(a => a.complex_type == "type"))
                {
                    if (prot.type.options.All(a => a.complex_type == "type") && prot.type.options.Length > 4)
                    {
                        foreach (var option in prot.type.options)
                        {
                            var opType = option.value.ToString();
                            var o = Types[opType];
                            if (o == null)
                                o = protApi.types.ToList().Find(a => a.name == opType);
                            if (o == null) throw new NotImplementedException();
                            
                            if (string.IsNullOrEmpty(o.parent))
                                o.parent = prot.name;
                            else
                            {
                                var a=DeepestParent(o);
                                if(a.name==prot.name)
                                 a.parent =prot.name;
                            }
                            
                        }
                    }
                    else
                    {
                        var t = GetType(prot.type, prot.name, sb, out _);
                        if (string.IsNullOrEmpty(prot.parent))
                            prot.parent = t;
                        else
                        {
                            DeepestParent(prot).parent = t;
                        }
                        var types= prot.type.options.Select(a => GetType(a, "", sb, out _)).ToList();
                        for (int i = 0; i <types.Count; i++)
                        {
                            var vale = types[i];
                            add += ($"public {prot.name}({vale} d) : base(d) {{}}\n");
                        }
                    }
                }

                sb.Append(
                    $"public {(prot.@abstract ? "abstract" : "")} partial class {prot.name} {(!string.IsNullOrEmpty(prot.parent) ? $" : {prot.parent} " : "")}");
                sb.AppendLine("{");
                sb.AppendLine(add);
                if (prot.properties != null)
                    foreach (var property in prot.properties)
                    {
                        string type = null;
                        type = GetType(property, sb, out var def);
                        if (type is "DataExtendMethod")
                        {
                            sb.AppendLine($"//can't implement {property.name} cause it's type is {type}");
                            continue;
                        }


                        if (type.Contains("-"))
                        {
                            sb.AppendLine("[ReplaceTireWithUnderscore]");
                            type = type.Replace("-", "_");
                        }


                        if (!string.IsNullOrEmpty(property.description))
                        {
                            sb.Append(
                                $"/// <summary>\n///{property.description.Replace("\n", "\n///")}\n/// </summary>\n");
                        }

                        if (def == null)
                            sb.AppendLine(
                                $"public {(property.@override ? "new" : "")} {type} @{property.name} {{get;set;}}");
                        else
                            sb.AppendLine(
                                $"public {type}{(property.optional ? "?" : "")} @{property.name} => {def.ToString().Replace("True", "true").Replace("False", "false")};");
                    }

                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine($"public enum {prot.name}{{");
                foreach (var ev in prot.values.OrderBy(a => a.order))
                {
                    if (!string.IsNullOrEmpty(ev.description))
                        sb.AppendLine($"///{ev.description.Replace("\n", "\n///")}");
                    sb.AppendLine($"{ev.name}={ev.order},");
                }

                sb.AppendLine($"}}");
            }
        }
        else
        {
            if (prot.name is "DataExtendMethod" or "string" or "bool" or "double" or "float" or "int8" or "int16"
                or "int32" or "int64" or "uint8" or "uint16" or "uint32" or "uint64")
            {
                sb.Append($"//Can't implement {prot.name};");
            }
            else if (prot.type.directValue == "builtin") throw new Exception("Not implemented!");
            else
            {
                if (!UseAlias)
                {
                    //public class TestID : TypedAlias<TestID, string> {public TestID(string val) : base(val) { } }
                    sb.Append(
                        $"public sealed class {prot.name} : TypedAlias<{prot.name},{BuiltinToCs(prot.type.directValue)}> {{   public {prot.name}({BuiltinToCs(prot.type.directValue)} val) : base(val){{}}  }}");
                }
                else sb.Append($"using {prot.name} = {BuiltinToCs(prot.type.directValue)};");
            }
        }

        gSb.AppendLine(sb.ToString());
    }

    public static bool UseAlias = false;

    private static string BuiltinToCs(string v)
    {
        return v switch
        {
            "int8" => typeof(System.SByte).FullName,
            "uint8" => typeof(Byte).FullName,
            "int16" => typeof(Int16).FullName,
            "int32" => typeof(Int32).FullName,
            "int64" => typeof(Int64).FullName,
            "uint16" => typeof(UInt16).FullName,
            "uint32" => typeof(UInt32).FullName,
            "uint64" => typeof(UInt64).FullName,
            _ => v
        };
    }

    private static PrototypeApi protApi;

    private static Dictionary<string, PrototypeApi.Prototype> Types = new Dictionary<string, PrototypeApi.Prototype>();
    public static void ProcessPrototypeJson(string path, string output)
    {
        var text = File.ReadAllText(path);
        protApi = text.FromJson<PrototypeApi>();
        StringBuilder gSb = new StringBuilder();
        gSb.AppendLine($"""
                        using FactorioModCS.Json;
                        using LuaNt;
                        using OneOf;
                        using boolean=System.Boolean;
                        using uint32=System.UInt32;
                        using uint16=System.UInt16;
                        using uint64=System.UInt64;
                        using int16=System.Int16;
                        using int8=System.SByte;
                        using uint8=System.Byte;
                        using int32=System.Int32;
                        using int64=System.Int64;
                        namespace Factorio;
                        """);

        foreach (var t in protApi.types)
        {
            Types[t.name] = t;
        }
        foreach (var t in protApi.prototypes)
        {
            Types[t.name] = t;
        }
        foreach (var t in protApi.types)
        {
            if (t.type != null && t.type.directValue != null)
                ProcessPrototype(t, gSb);
        }

        foreach (var t in protApi.types)
        {
            if (!(t.type != null && t.type.directValue != null))
                ProcessPrototype(t, gSb);
        }

        gSb.AppendLine("public static class defines{");

        foreach (var t in protApi.defines)
        {
            ProcessPrototype(t, gSb);
        }

        gSb.AppendLine("}");

        foreach (var t in protApi.prototypes)
        {
            ProcessPrototype(t, gSb);
        }

        File.WriteAllText(output, gSb.ToString());
    }
}