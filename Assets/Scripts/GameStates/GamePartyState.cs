using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class GamePartyState : State<GameController>
{
    [SerializeField] PartyScreen partyScreen;

    public static GamePartyState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    GameController gameController;

    public override void Enter(GameController owner)
    {
        gameController = owner;
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
        if(selection == 0)
        {
            gameController.StateMachine.Push(GamePartyState.i);
        }
    }

    public override void Exit()
    {
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnPokemonSelected;
        partyScreen.OnBack -= OnBack;
    }

     void OnBack()
    {
        gameController.StateMachine.Pop();
    }
}
