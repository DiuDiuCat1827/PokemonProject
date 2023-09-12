using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class BattleState : State<GameController>
{
    [SerializeField] BattleSystem battleSystem;

    //Input

    public BattleTrigger trigger { get; set; }

    public TrainerControler trainer { get; set; }

    public static BattleState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    GameController gameController;

    public override void Enter(GameController owner)
    {
        gameController = owner;

        battleSystem.gameObject.SetActive(true);
        gameController.WorldCamera.gameObject.SetActive(false);

        var playerParty = gameController.PlayerController.GetComponent<PokemonParty>();

        if( trainer == null)
        {
           var wildPokemon = gameController.CurrentScene.GetComponent<MapArea>().GetRandomWildPokemon(trigger);
           var wildPokemonCopy = new Pokemon(wildPokemon.Base, wildPokemon.Level);
           battleSystem.StartBattle(playerParty, wildPokemonCopy, trigger);
        }
        else
        {
            var trainerParty = trainer.GetComponent<PokemonParty>();
            battleSystem.StartTrainerBattle(playerParty, trainerParty);
        }

        battleSystem.OnBattleOver += EndBattle;
    }

    public override void Exit()
    {
        battleSystem.gameObject.SetActive(false);
        gameController.WorldCamera.gameObject.SetActive(true);

        battleSystem.OnBattleOver -= EndBattle;
    }

    public override void Execute()
    {
        battleSystem.HandleUpdate();
    }

    void EndBattle(bool win)
    {
        if (trainer != null && win == true)
        {
            trainer.BattleLost();
            trainer = null;
        }
        gameController.StateMachine.Pop();
    }
}
