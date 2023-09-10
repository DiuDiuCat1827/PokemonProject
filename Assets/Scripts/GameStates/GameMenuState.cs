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

    }

    public override void Execute()
    {
        menuController.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.K))
        {
            gameController.StateMachine.Pop();
        }
    }

    public override void Exit()
    {
        menuController.gameObject.SetActive(false);
    }
}
