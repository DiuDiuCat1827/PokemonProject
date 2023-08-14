using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Create new pokeball")]
public class PokeballItem : ItemBase
{
    public override bool Use(Pokemon pokemon)
    {
        return true;
    }
}
