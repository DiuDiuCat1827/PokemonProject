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
        if (selection == 0)
        {
            //Fight
            battleSystem.SelectedAction = BattleAction.Move;
            MoveSelectionState.i.Moves = battleSystem.PlayerUnit.Pokemon.Moves;
            battleSystem.StateMachine.ChangeState(MoveSelectionState.i);
        }
        else if (selection == 1)
        {
            // Bag
        }
        else if (selection == 2)
        {
            // Pokemon
            StartCoroutine(GoToPartyState());
        }
        else if (selection == 3)
        {
            // Run
            battleSystem.SelectedAction = BattleAction.Run;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        }

    }

    IEnumerator GoToPartyState()
    {
        yield return GameController.Instance.StateMachine.PushAndWait(PartyState.i);
        var selectedPokemon = PartyState.i.SelectedPokemon;
        if(selectedPokemon != null)
        {
            battleSystem.SelectedAction = BattleAction.SwitchPokemon;
            battleSystem.SelectedPokemon = selectedPokemon;
            battleSystem.StateMachine.ChangeState(RunTurnState.i);
        }
    }
}
