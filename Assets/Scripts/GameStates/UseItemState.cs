using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class UseItemState : State<GameController>
{
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    public static UseItemState i { get; private set; }

    Inventory inventory;

    private void Awake()
     {
        i = this;
        inventory = Inventory.GetInventory();
     }

    GameController gameController;

    public override void Enter(GameController owner)
    {
        gameController = owner;

        StartCoroutine(UseItem());
    }

    IEnumerator UseItem()
    {     
        var item = inventoryUI.SelectedItem;
        var pokemon = partyScreen.Selectedmember;

        if(item is TmItem)
        {
            yield return HandleTmItems();
        }
        else
        {
            // handle Evolution Items
            if (item is EvolutionItem)
            {
                var evolution = pokemon.CheckForEvolution(item);
                if (evolution != null)
                {
                    yield return EvolutionManager.i.Evolve(pokemon, evolution);
                }
                else
                {
                    yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
                    gameController.StateMachine.Pop();
                    yield break;
                }
            }

            var useItem = inventory.UseItem(item, partyScreen.Selectedmember);
            if (useItem != null)
            {
                if (useItem is RecoveryItem)
                {
                    yield return DialogManager.Instance.ShowDialogText($"The Player used {useItem.Name}");
                }
            }
            else
            {
                if (inventoryUI.SelectedCategory == (int)ItemCategory.Items)
                {
                    yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
                }

            }
        }
        gameController.StateMachine.Pop();

    }

    IEnumerator HandleTmItems()
    {
        var tmItem = inventoryUI.SelectedItem as TmItem;

        if (tmItem == null)
        {
            yield break;
        }

        var pokemon = partyScreen.Selectedmember;
        if (pokemon.HasMove(tmItem.Move))
        {
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} already learned {tmItem.Move.Name}!");
            yield break;
        }

        if (!tmItem.CanBeTaught(pokemon))
        {
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} can't learned {tmItem.Move.Name}!");
            yield break;
        }

        if (pokemon.Moves.Count < PokemonBase.MaxNumOfMoves)
        {
            pokemon.LearnMove(tmItem.Move);
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} learned {tmItem.Move.Name}!");
        }
        else
        {
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} is trying to learn {tmItem.Move.Name}!");
            yield return DialogManager.Instance.ShowDialogText($"But it cannot learn more than {PokemonBase.MaxNumOfMoves} moves");
            //yield return ChooseMoveToForget(pokemon, tmItem.Move);
            //yield return new WaitUntil(() => inventoryState != InventoryUIState.MoveToForget);
        }
    }


}
