For example it can turn
```cs
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
    data.extend([fireArmor, recipe]);
};
```
into
```lua
local v_0 = table.deepcopy(data.raw["armor"]["heavy-armor"])
v_0.name = "fire-armor"
v_0.icons = {{icon = v_0.icon, icon_size = v_0.icon_size, tint = {r = 1, g = 0, b = 0, a = 0,3}}}
v_0.resistances = {{type = "physical", decrease = 6, percent = 10}, {type = "explosion", decrease = 10, percent = 30}, {type = "acid", decrease = 5, percent = 30}, {type = "fire", decrease = 0, percent = 100}}
local v_2 = {}
v_2.type = "recipe"
v_2.name = "fire-armor"
v_2.enabled = 1
v_2.energy_required = 8
v_2.ingredients = {{name = "copper-plate", amount = 200, type = "item"}, {name = "steel-plate", amount = 50, type = "item"}}
v_2.results = {{name = "fire-armor", amount = 1, type = "item"}}
local v_1 = v_2
data.extend({v_0, v_1})
```
And all of that AT RUNTIME
