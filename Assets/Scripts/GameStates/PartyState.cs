using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class PartyState : State<GameController>
{
    [SerializeField] PartyScreen partyScreen;

    public Pokemon SelectedPokemon { get; private set; }

    public static PartyState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    GameController gameController;

    public override void Enter(GameController owner)
    {
        gameController = owner;

        SelectedPokemon = null;
        partyScreen.gameObject.SetActive(true);
        partyScreen.OnSelected += OnPokemonSelected;
        partyScreen.OnBack += OnBack;
    }

    public override void Execute()
    {
        partyScreen.HandleUpdate();
    }

    void OnPokemonSelected(int selection)
    {
        SelectedPokemon = partyScreen.Selectedmember;

        var prevState = gameController.StateMachine.GetPrevState();
        if(prevState == InventoryState.i)
        {
            //Use Item
            StartCoroutine(GoToUseItemState());
        }
        else if(prevState == BattleState.i){

            var battleState = prevState as BattleState;
            
            if (SelectedPokemon.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted pokemon");
                return;
            }
            if (SelectedPokemon == battleState.BattleSystem.PlayerUnit.Pokemon)
            {
                partyScreen.SetMessageText("You can't switch with the pokemon");
                return;
            }

            gameController.StateMachine.Pop();
        }
        else{
            //TODO open summary screen
            Debug.Log($"Selected pokemon ai index {selection} ");
        }
        
    }

    IEnumerator GoToUseItemState()
    {
        yield return gameController.StateMachine.PushAndWait(UseItemState.i);
        gameController.StateMachine.Pop();
    }

    public override void Exit()
    {
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPokemonSelected;
        partyScreen.OnBack -= OnBack;
    }

     void OnBack()
    {
        SelectedPokemon = null;

        var prevState = gameController.StateMachine.GetPrevState();
        if(prevState == BattleState.i)
        {
            var battleState = prevState as BattleState;
            if (battleState.BattleSystem.PlayerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SetMessageText("You have to choose a pokemon to continue");
                return;
            }
        }

        gameController.StateMachine.Pop();
    }


}
