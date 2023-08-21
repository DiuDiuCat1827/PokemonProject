using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Create new TM or HM")]
public class TmItem : ItemBase
{
    [SerializeField] MoveBase move;
    [SerializeField] bool isHM;

    public override string Name => base.Name + $": {move.Name}";
    
    public bool CanBeTaught(Pokemon pokemon)
    {
        Debug.Log(pokemon.Base.LearnableByItems.Count);
        Debug.Log(pokemon.Base.Name);
        Debug.Log(move.Name);
        return pokemon.Base.LearnableByItems.Contains(move);
    }

    public override bool Use(Pokemon pokemon)
    {
        //Learn move is handled from Inventory UI,If it was learned then return true
        return pokemon.HasMove(move);
    }

    public override bool IsReusable => isHM;

    public override bool CanUseInBattle => false;

    public MoveBase Move => move;
}
