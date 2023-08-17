using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Create new pokeball")]
public class PokeballItem : ItemBase
{
    [SerializeField] float catchRateModifier = 1;

    public override bool Use(Pokemon pokemon)
    {
        if(GameController.Instance.State == GameState.Battle)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
    public float CatchRateModifier => catchRateModifier;
}
