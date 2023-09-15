using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class AboutToUseState : State<BattleSystem>
{
    public Pokemon NewPokemon { get; set; }

    bool aboutToUseChoice;

    public static AboutToUseState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    BattleSystem battleSystem;

    public override void Enter(BattleSystem owner)
    {
        battleSystem = owner;
        StartCoroutine(StartState());
    }

    IEnumerator StartState()
    {
        yield return battleSystem.DialogBox.TypeDialog($"{battleSystem.Trainer.Name} is about to use {NewPokemon.Base.Name}.Do you want to change pokemon");
        battleSystem.DialogBox.EnableChoiceBox(true);
    }

    public override void Execute()
    {
        if(!battleSystem.DialogBox.IsChoiceBoxEnabled)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
        {
            aboutToUseChoice = !aboutToUseChoice;
        }
        battleSystem.DialogBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKey(KeyCode.J))
        {
            battleSystem.DialogBox.EnableChoiceBox(false);
            if (aboutToUseChoice == true)
            {
                //Yes
                StartCoroutine(SwitchAndContinueBattle());
            }
            else
            {
                //No 
                StartCoroutine(ContinueBattle());
            }
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            battleSystem.DialogBox.EnableChoiceBox(false);
            StartCoroutine(ContinueBattle());
        }
    }

    IEnumerator SwitchAndContinueBattle()
    {
        yield return GameController.Instance.StateMachine.PushAndWait(PartyState.i);
        var selectedPokemon = PartyState.i.SelectedPokemon;
        if(selectedPokemon != null)
        {

            yield return battleSystem.SwitchPokemon(selectedPokemon);
        }

        yield return ContinueBattle();
    }

    IEnumerator ContinueBattle()
    {
        yield return battleSystem.SendNextTrainerPokemon();
        battleSystem.StateMachine.Pop();
    }
}
