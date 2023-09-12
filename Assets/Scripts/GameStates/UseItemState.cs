using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;
using System.Linq;

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
            
            yield return DialogManager.Instance.ShowDialogText($"Choose a move you wan't to forget ", true, false);

            MoveToForgetState.i.NewMove = tmItem.Move;
            MoveToForgetState.i.CurrentMoves = pokemon.Moves.Select(m => m.Base).ToList();
            yield return gameController.StateMachine.PushAndWait(MoveToForgetState.i);

            int moveIndex = MoveToForgetState.i.Selection;
          

            if (moveIndex == PokemonBase.MaxNumOfMoves || moveIndex == -1)
            {
                //Do not learn the new move
                yield return StartCoroutine(DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} did not learn {tmItem.Move}"));
            }
            else
            {
                //Forget the selected move and learn new move 
                var selectedMove = pokemon.Moves[moveIndex].Base;
                yield return StartCoroutine(DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} forgot {selectedMove.Name} and learn {tmItem.Move.Name}"));
                //forget the select move and learn new move
                pokemon.Moves[moveIndex] = new Move(tmItem.Move);
            }
        }
    }


}
