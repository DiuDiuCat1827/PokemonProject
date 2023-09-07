using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class GameMenuState : State<GameController>
{
    public static GameMenuState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    GameController gameController;
    public override void Enter(GameController owner)
    {
        gameController = owner;

    }

    public override void Execute()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            gameController.StateMachine.Pop();
        }
    }
}
