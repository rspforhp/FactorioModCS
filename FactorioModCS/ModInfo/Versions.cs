namespace FactorioModCS;

public record MajorMiddleMinorVersion
{
    public readonly short Major;
    public readonly short Middle;
    public readonly short Minor;
    public MajorMiddleMinorVersion(short major, short middle, short minor)
    {
        Major = major;
        Middle = middle;
        Minor = minor;
    }
    public MajorMiddleMinorVersion(string str)
    {
        if (str.Count(a => a == '.') == 1)
        {
            MajorMinorVersion v = str;
            Major = v.Major;
            Middle = -1;
            Minor = v.Minor;
        }
        else
        {
            var split = str.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (split.Length != 3) throw new Exception($"Failed to parse version string {str}!");
            try
            {
                Major = short.Parse(split[0]);
                Middle = short.Parse(split[1]);
                Minor = short.Parse(split[2]);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse version string {str}! With error {e.Message}!");
            }
        }
     
    }

    public override string ToString()
    {
        if (Middle == -1)
        {
            return $"{Major}.{Minor}";
        }
        return $"{Major}.{Middle}.{Minor}";
    }
    public static implicit operator MajorMiddleMinorVersion(string str)=> new MajorMiddleMinorVersion(str);
    public static implicit operator string(MajorMiddleMinorVersion str)=>str.ToString();
        
}
public record MajorMinorVersion
{
    public readonly short Major;
    public readonly short Minor;
    public MajorMinorVersion(short major, short middle, short minor)
    {
        Major = major;
        Minor = minor;
    }
    public MajorMinorVersion(string str)
    {
        var split = str.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (split.Length != 2) throw new Exception($"Failed to parse version string {str}!");
        try
        {
            Major = short.Parse(split[0]);
            Minor = short.Parse(split[1]);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to parse version string {str}! With error {e.Message}!");
        }
    }
    public override string ToString()=>$"{Major}.{Minor}";
    public static implicit operator MajorMinorVersion(string str)=> new MajorMinorVersion(str);
    public static implicit operator string(MajorMinorVersion str)=>str.ToString();
        
}