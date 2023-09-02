using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public  enum GameState { FreeRoam,Battle, Dialog, Menu,PartyScreen, Bag, Cutscene,Paused, Evolution, Shop }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    TrainerControler trainer;

    bool battleLost = false;

    GameState state;

    GameState prevState;

    GameState stateBeforeEvolution;

    MenuController menuController;

    public SceneDetails CurrentScene { get; private set; }
    public SceneDetails PrevScene { get; private set; }

    public static GameController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        menuController = GetComponent<MenuController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PokemonDB.Init();
        MoveDB.Init();
        ConditionData.Init();
        ItemDB.Init();
        QuestDB.Init();
    }

    // Start is called before the first frame update
    private  void Start()
    {
        battleSystem.OnBattleOver += EndBattle;

        partyScreen.Init();

        DialogManager.Instance.OnShowDialog += () =>
        {
            prevState = state;
            state = GameState.Dialog;
        };

        DialogManager.Instance.OnDialogFinished += () =>
        {
            if (state == GameState.Dialog)
            state = prevState;
        };

        menuController.onBack += () =>
        {
            state = GameState.FreeRoam;
        };

        menuController.onMenuSelected += OnMenuSelected;

        EvolutionManager.i.OnStartEvolution += () =>{
            stateBeforeEvolution = state;
            state = GameState.Evolution;
        };
        EvolutionManager.i.OnCompleteEvolution += () =>{
            partyScreen.SetPartyData();
            state = stateBeforeEvolution;
        }; 

        ShopController.i.OnStart += ()=> state = GameState.Shop;
        ShopController.i.OnFinish += ()=> state = GameState.FreeRoam;
    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            prevState = state;
            state = GameState.Paused;
        }
        else
        {
            state = prevState;
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
        var wildPokemon = CurrentScene.GetComponent<MapArea>().GetRandomWildPokemon();

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

        partyScreen.SetPartyData();

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);

        var playerParty = playerController.GetComponent<PokemonParty>();
        StartCoroutine(playerParty.CheckForEvolutions());
    }

    // Update is called once per frame
    private void Update()
    {
        if(state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
            if (Input.GetKeyDown(KeyCode.Return))
            {
                menuController.OpenMenu();
                state = GameState.Menu;
            }

       

        }else if(state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
        else if(state == GameState.Dialog) {
            DialogManager.Instance.HandleUpdate();
        }else if(state == GameState.Menu)
        {
            menuController.HandleUpdate();
        }else if(state == GameState.PartyScreen)
        {
            
            Action onSelected = () => {
                //TODO:Go to Summary Screen
            };
            
            Action onBack= () =>{
                partyScreen.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };
            partyScreen.HandleUpdate(onSelected, onBack);
        }else if(state == GameState.Bag)
        {
            Action onBack= () =>{
                inventoryUI.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };
            inventoryUI.HandleUpdate(onBack);
        }else if(state == GameState.Shop)
        {
            ShopController.i.HandleUpdate();
        }

        
    }

    public void SetCurrentScene(SceneDetails currScene)
    {
        PrevScene = CurrentScene;
        CurrentScene = currScene;
    }

    void OnMenuSelected(int selectedItem)
    {
        if (selectedItem == 0)
        {
            //Pokemon
            partyScreen.SetPartyData();
            partyScreen.gameObject.SetActive(true);
            state = GameState.PartyScreen;
        }else if(selectedItem == 1)
        {
            //Bag
            inventoryUI.gameObject.SetActive(true);
            state = GameState.Bag;
        }else if(selectedItem == 2)
        {
            SavingSystem.i.Save("saveSlot1");
            state = GameState.FreeRoam;
        }else if(selectedItem == 3)
        {
            SavingSystem.i.Load("saveSlot1");
            state = GameState.FreeRoam;
        }


    }

    public IEnumerator MoveCamera(Vector2 moveOffset, bool waitForFaderOut = false)
    {
        yield return Fader.i.FadeIn(0.5f);

        worldCamera.transform.position += new Vector3(moveOffset.x, moveOffset.y);

        if (waitForFaderOut)
        {
            yield return Fader.i.FadeOut(0.5f);
        }
        else
        {
            StartCoroutine(Fader.i.FadeOut(0.5f));
        }

    }

    public GameState State => state;
}
