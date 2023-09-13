using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class MoveSelectionState : State<BattleSystem>
{
   [SerializeField] MoveSelectionUI  selectionUI;
   [SerializeField] GameObject moveDetailsUI;

    public List<Move> Moves { get; set; }

    public static MoveSelectionState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    BattleSystem battleSystem;

    public override void Enter(BattleSystem owner)
    {
        battleSystem = owner;

        selectionUI.SetMoves(Moves);

        selectionUI.gameObject.SetActive(true);
        selectionUI.OnSelected += OnMoveSelected;
        selectionUI.OnBack += OnBack;

        moveDetailsUI.SetActive(true);
        battleSystem.DialogBox.EnableDialogText(false);
    }

    public override void Execute()
    {
        selectionUI.HandleUpdate();
    }

    public override void Exit()
    {
        selectionUI.gameObject.SetActive(false);
        selectionUI.OnSelected -= OnMoveSelected;
        selectionUI.OnBack -= OnBack;

        moveDetailsUI.SetActive(false);
        battleSystem.DialogBox.EnableDialogText(true);
    }

    void OnMoveSelected(int selection)
    {

    }

    void OnBack()
    {
        battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
    }
}
