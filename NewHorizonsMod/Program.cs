using static Globals;
using Factorio;
using FactorioModCS;
//FactorioModCompiler.GenerateCode(Path.Combine(@"C:\Users\petke\RiderProjects\FactorioModCS\FactorioModCS\","Generated"));
//TODO: parse runtime

Console.WriteLine("Hello, World!");
FactorioModCompiler.SetModsOutput(@"C:\Users\petke\AppData\Roaming\Factorio\mods\");
FactorioModCompiler.TryAddMod(new FactorioMod("new-horizons","0.0.1","New Horizons","Miya_Kopie", factorio_version:"2.0",description:"IDK YET!",dependencies: ["base >= 2.0"]),out var m);
Console.WriteLine(m);
m.Data += delegate()
{
    var fireArmor = (ArmorPrototype)table.deepcopy(data.raw["armor"]["heavy-armor"]); // copy the table that defines the heavy armor item into the fireArmor variable
    fireArmor.name = "fire-armor";
    fireArmor.icons =
    [
        new(){
        icon = fireArmor.icon,
        icon_size = fireArmor.icon_size,
        tint =  new(){r = 1, g = 0, b = 0, a = 0.3f} },
    ];
    fireArmor.resistances =
    [
        new(){
        type = new("physical"),
        decrease = 6,
        percent = 10
        },
        new(){
        type =  new("explosion"),
        decrease = 10,
        percent = 30
        },
        new(){
        type =  new("acid"),
        decrease = 5,
        percent = 30
        },
        new(){
        type =  new("fire"),
        decrease = 0,
        percent = 100
        }
    ];
    // create the recipe prototype from scratch
    var recipe = new RecipePrototype()
    {
        type = "recipe",
        name = "fire-armor",
        enabled = true,
        energy_required = 8, // time to craft in seconds (at crafting speed 1)
        ingredients =
        [
            (IngredientPrototype)new ItemIngredientPrototype() { name = new("copper-plate"), amount = 200 },
            (IngredientPrototype)new ItemIngredientPrototype() { name = new("steel-plate"), amount = 50 }
        ],
        results = [(ProductPrototype)new ItemProductPrototype(){name =new("fire-armor"), amount = 1}]
    };
    for (int i = 0; i < 8; i++)
    {
        recipe.energy_required+=1;
    }
    data.extend([fireArmor, recipe]);
};
FactorioModCompiler.Compile();
