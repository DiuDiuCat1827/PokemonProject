using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public  enum GameState { FreeRoam,Battle, Dialog, Cutscene,Paused }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    TrainerControler trainer;

    bool battleLost = false;

    GameState state;

    GameState stateBeforePause;

    public static GameController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ConditionData.Init();
    }

    // Start is called before the first frame update
    private  void Start()
    {
        battleSystem.OnBattleOver += EndBattle;


        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };

        DialogManager.Instance.OnCloseDialog += () =>
        {
            if (state == GameState.Dialog)
            state = GameState.FreeRoam;
        };
    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            stateBeforePause = state;
            state = GameState.Paused;
        }
        else
        {
            state = stateBeforePause;
        }
    }

    public void OnEnterTrainerView(TrainerControler trainer)
    {
        state = GameState.Cutscene;
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }

    public void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var wildPokemon = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildPokemon();

        var wildPokemonCopy = new Pokemon(wildPokemon.Base,wildPokemon.Level);

        battleSystem.StartBattle(playerParty, wildPokemonCopy);
    }

    public void StartTrainerBattle(TrainerControler trainer)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        this.trainer = trainer;
        var playerParty = playerController.GetComponent<PokemonParty>();
        var trainerParty = trainer.GetComponent<PokemonParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }



    void EndBattle(bool win)
    {
        if (trainer != null && win == true){
            trainer.BattleLost();
            trainer = null;
        }

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
    }

    // Update is called once per frame
    private void Update()
    {
        if(state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
        }else if(state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
        else if(state == GameState.Dialog) {
            DialogManager.Instance.HandleUpdate();
        }
    }
}
