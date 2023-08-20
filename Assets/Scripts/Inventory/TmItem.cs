using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Create new TM or HM")]
public class TmItem : ItemBase
{
    [SerializeField] MoveBase move;

    public override bool Use(Pokemon pokemon)
    {
        //Learn move is handled from Inventory UI,If it was learned then return true
        return pokemon.HasMove(move);
    } 

    public MoveBase Move => move;
}
