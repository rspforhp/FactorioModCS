Loop example
```cs
 for (int i = 0; i < 8; i++)
    {
        recipe.energy_required+=1;
    }
```
into
```lua
v_3 = 0
goto _593_
::_632_::
v_1.energy_required = v_1.energy_required + 1
v_3 = v_3 + 1
::_593_::
v_4 = v_3 < 8
if  (v_4) then
	goto _632_
end
```
