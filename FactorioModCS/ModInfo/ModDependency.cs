using System.Numerics;

namespace FactorioModCS;

public record ModDependency
{
    public enum DependencyType
    {
        NONE,
        Incompatibility,
        Optional,
        HiddenOptional,
        LoadOrderDoesntMatter,
        Dependant,
    }

    public enum EqualityOperator
    {
        NONE,
        Smaller,
        SmallerOrEqual,
        Equal,
        BiggerOrEqual,
        Bigger,
    }

    
    public static string DepTypeToString(DependencyType str)
    {
        switch (str)
        {
            case DependencyType.Incompatibility: return  "!";
            case   DependencyType.Optional : return "?";
            case DependencyType.HiddenOptional : return "(?)";
            case  DependencyType.LoadOrderDoesntMatter : return "~";
            case  DependencyType.Dependant : return "";
        }
        throw new Exception($"Failed to parse dependency type operator {str}!");
    }
    public static string CompareToString(EqualityOperator str)
    {
        switch (str)
        {
            case EqualityOperator.Smaller : return "<";
            case EqualityOperator.SmallerOrEqual : return "<=";
            case EqualityOperator.Equal : return "=";
            case EqualityOperator.BiggerOrEqual : return ">=";
            case EqualityOperator.Bigger: return ">";
        }
        throw new Exception($"Failed to parse compare operator {str}!");
    }
    
    
    public static DependencyType DepTypeFromString(string str)
    {
        switch (str)
        {
            case "!": return DependencyType.Incompatibility;
            case "?": return DependencyType.Optional;
            case "(?)": return DependencyType.HiddenOptional;
            case "~": return DependencyType.LoadOrderDoesntMatter;
            case "": return DependencyType.Dependant;
        }
        throw new Exception($"Failed to parse dependency type operator {str}!");
    }
    public static EqualityOperator CompareFromString(string str)
    {
        switch (str)
        {
            case "<": return EqualityOperator.Smaller;
            case "<=": return EqualityOperator.SmallerOrEqual;
            case "=": return EqualityOperator.Equal;
            case ">=": return EqualityOperator.BiggerOrEqual;
            case ">": return EqualityOperator.Bigger;
        }
        throw new Exception($"Failed to parse compare operator {str}!");
    }

    public readonly DependencyType Type;
    public readonly string ModName;
    public readonly EqualityOperator Operator;
    public readonly MajorMiddleMinorVersion ModVersion;

    public ModDependency(DependencyType type, string modName, EqualityOperator @operator, MajorMiddleMinorVersion modVersion)
    {
        Type = type;
        ModName = modName;
        Operator = @operator;
        ModVersion = modVersion;
    }

    public ModDependency(string str)
    {
        bool hasPrefix =( str[0] is '!' or '?'  or '~') || (str[0] is '(' && str[1] is '?' && str[2] is ')');
        var split = str.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Type = hasPrefix ? DepTypeFromString(split[0]) : DependencyType.Dependant;
        if (hasPrefix) split = split[1..];
        ModName = split[0];
        Operator = CompareFromString(split[1]);
      
         ModVersion = split[2];  
    }

    public override string ToString()
    {
        var prefix = DepTypeToString(Type);
        if (prefix != "") prefix += " ";
        return $"{prefix}{ModName} {CompareToString(Operator)} {ModVersion}";
    }

    public static implicit operator ModDependency(string str)
    {
        return new ModDependency(str);
    }
    public static implicit operator string(ModDependency str)
    {
        return str.ToString();
    }
}