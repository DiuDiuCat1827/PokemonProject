using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class GameMenuState : State<GameController>
{
    [SerializeField] MenuController menuController;

    public static GameMenuState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    GameController gameController;
    public override void Enter(GameController owner)
    {
        gameController = owner;
        menuController.gameObject.SetActive(true);
        menuController.OnSelected += OnMenuItemSelected;
        menuController.OnBack += OnBack;
    }

    public override void Execute()
    {
        menuController.HandleUpdate();

        
    }

    public override void Exit()
    {
        menuController.gameObject.SetActive(false);
        menuController.OnSelected -= OnMenuItemSelected;
        menuController.OnBack -= OnBack;
    }

    void OnMenuItemSelected(int selection)
    {
        if(selection == 0)
        {
            gameController.StateMachine.Push(GamePartyState.i);
        }else if (selection == 1)
        {
            //Bag
            gameController.StateMachine.Push(InventoryState.i);
        }
    }

    void OnBack()
    {
        gameController.StateMachine.Pop();
    }
}
