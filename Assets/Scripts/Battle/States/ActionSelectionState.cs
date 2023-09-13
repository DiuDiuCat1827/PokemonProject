using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDE.GenericSelectionUI;
using GDEUtils.StateMachine;

public class ActionSelectionState : State<BattleSystem>
{
    [SerializeField] ActionSelectionUI selectionUI;

    public static ActionSelectionState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    BattleSystem battleSystem;

    public override void Enter(BattleSystem owner)
    {
        battleSystem = owner;
        selectionUI.gameObject.SetActive(true);
        selectionUI.OnSelected += OnActionSelected;

        battleSystem.DialogBox.SetDialog("Choose an action");
    }

    public override void Execute()
    {
        selectionUI.HandleUpdate();
    }

    public override void Exit()
    {
        selectionUI.gameObject.SetActive(false);
        selectionUI.OnSelected -= OnActionSelected;
    }

    void OnActionSelected(int selection)
    {
        if( selection == 0)
        {
            //Fight
            MoveSelectionState.i.Moves = battleSystem.PlayerUnit.Pokemon.Moves;
            battleSystem.StateMachine.ChangeState(MoveSelectionState.i);
        }
    }
}
