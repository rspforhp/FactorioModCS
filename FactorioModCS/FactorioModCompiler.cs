using FactorioModCS.Json;

namespace FactorioModCS;

public static class FactorioModCompiler
{
    public static void GenerateCode(string generateTo)
    {
        var protType = @"C:\Users\petke\RiderProjects\FactorioModCS\FactorioModCS\Json\prototype-api.json";
        CodeGeneration.ProcessPrototypeJson(protType,Path.Combine(generateTo,"Prototypes.g.cs"));
    }
    private static readonly SortedSet<FactorioMod> modsToCompile = new SortedSet<FactorioMod>();
    private static string modsOutput;

    public static void SetModsOutput(string str)
    {
        modsOutput = str;
    }
    public static bool TryAddMod(FactorioMod mod, out FactorioMod res)
    {
        if (modsToCompile.Add(mod))
        {
            res = mod;
            return true;
        }
        res = null;
        return false;
    }

    public static void Compile()
    {
        //TODO: make this thread-safe
        foreach(var mod in modsToCompile)
        {
            var modFolder = Path.Combine(modsOutput, mod.Info.name);
            //Clean-up fully, cause i dont' wanna implement staged compilation **yet**
            if (!Directory.Exists(modFolder)) Directory.CreateDirectory(modFolder);
            else
            {
                Directory.Delete(modFolder,true);
                Directory.CreateDirectory(modFolder);
            }
            //
            var infoJson=Path.Combine(modFolder, "info.json");
            File.WriteAllText(infoJson,mod.Info.ToString());

            mod.Data.ModPath = modFolder;
            mod.Data.Compile();


        };
    }
}