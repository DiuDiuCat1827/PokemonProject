using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using GDEUtils.StateMachine;

public enum BattleStates { Start, ActionSelection, MoveSelection, RunningTurn, Busy ,PartyScreen ,AboutToUse,MoveToForget, BattleOver,Bag, None}

public enum BattleAction { Move,SwitchPokemon,UseItem,Run}

public enum BattleTrigger { LongGrass, Water}

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;

    [SerializeField] BattleUnit enemyUnit;

    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;
    [SerializeField] MoveToForgetSelectionUI moveSelectionUI;
    [SerializeField] InventoryUI inventoryUI;
    MoveBase moveToLearn;

    [Header("Audio")]
    [SerializeField] AudioClip wildBattleMusic;
    [SerializeField] AudioClip trainerBattleMusic;
    [SerializeField] AudioClip battleVictoryMusic;

    [Header("BackGround Images")]
    [SerializeField] Image backgroundImage;
    [SerializeField] Sprite grassBackground;
    [SerializeField] Sprite waterBackground;

    public StateMachine<BattleSystem> StateMachine { get;private set;}

    public event Action<bool> OnBattleOver;

    public int SelectedMove { get; set;}

    public BattleAction SelectedAction { get; set;}

    public Pokemon SelectedPokemon { get; set;}

    public bool IsBattleOver { get; private set;}

    BattleStates state; 

    int currentAction;
    int currentMove;
    bool aboutToUseChoice = true;

    public PokemonParty PlayerParty { get;private set;}
    public PokemonParty TrainerParty { get;private set;}
    public Pokemon WildPokemon { get;private set;}

    public bool IsTrainerBattle {get; private set;} = false;
    PlayerController player;
    TrainerControler trainer;

    BattleTrigger battleTrigger;

    public int escapeAttempts {get;set;}

    public void StartBattle(PokemonParty playerParty,Pokemon wildPokemon,
        BattleTrigger trigger = BattleTrigger.LongGrass)
    {
        this.PlayerParty = playerParty;
        this.WildPokemon = wildPokemon;
        player = playerParty.GetComponent<PlayerController>();
        IsTrainerBattle = false;

        battleTrigger = trigger;

        AudioManager.i.PlayMusic(wildBattleMusic);
        StartCoroutine(SetupBattle());

    }

    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty,
        BattleTrigger trigger = BattleTrigger.LongGrass)
    {
        this.PlayerParty = playerParty;
        this.TrainerParty = trainerParty;

        IsTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerControler>();

        battleTrigger = trigger;

        AudioManager.i.PlayMusic(trainerBattleMusic,fade:true);
        StartCoroutine(SetupBattle());
    }


    public void BattleOver(bool win)
    {
        IsBattleOver = true;
        PlayerParty.Pokemons.ForEach(p => p.OnBattleOver());
        playerUnit.Hud.ClearData();
        enemyUnit.Hud.ClearData();
        OnBattleOver(win);
    }


    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit,BattleUnit targetUnit,Move move)
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();

        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
            
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {
          sourceUnit.PlayAttackAnimation();
          AudioManager.i.PlaySfx(move.Base.Sound);

          yield return new WaitForSeconds(1f);
          targetUnit.PlayHitAnimation();
          AudioManager.i.PlaySfx(AudioId.Hit);

          if (move.Base.Category == MoveCategory.Status)
          {
            yield return RunMoveEffects(move.Base.Effects,sourceUnit.Pokemon,targetUnit.Pokemon,move.Base.Target);
          }
          else
          {
            var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.WaitForHPUpdate();
            yield return ShowDamageDetails(damageDetails);
          }

          if(move.Base.Secondaries !=null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                foreach(var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if(rnd<= secondary.Chance){
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon,secondary.Target);
                    }
                }
            }
            {

            }

          if (targetUnit.Pokemon.HP <= 0)
          {
               yield return HandlePokemonFainted(targetUnit);
          }
        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed");
        }

 
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleStates.BattleOver) yield break;
        yield return new WaitUntil(()=> state == BattleStates.RunningTurn);
        yield return sourceUnit.Hud.WaitForHPUpdate();

        //Status like burn or psn will hurt pokemon after the turn 
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return dialogBox.TypeDialog($"{ sourceUnit.Pokemon.Base.Name}Fainted");
            sourceUnit.PlayFaintAnimation();

            yield return new WaitForSeconds(2f);
            CheckForBattleOver(sourceUnit);
            yield return new WaitUntil(() => state == BattleStates.RunningTurn);
        }
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{ faintedUnit.Pokemon.Base.Name}Fainted");
        faintedUnit.PlayFaintAnimation();

        yield return new WaitForSeconds(2f);

        if (!faintedUnit.IsPlayerUnit)
        {
            bool battleWon = true;
            if (IsTrainerBattle)
            {
                battleWon = TrainerParty.GetHealthyPokemon() == null;
            }
            if (battleWon)
            {
                AudioManager.i.PlayMusic(battleVictoryMusic,fade:true);
            }

            int expYield = faintedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;

            float trainerBonus = (IsTrainerBattle)? 1.5f:1f;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Pokemon.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} exp");

            yield return playerUnit.Hud.SetExpSmooth();

            while (playerUnit.Pokemon.CheckForLevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} grew to lv {playerUnit.Pokemon.Level}");

                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrLevel();
                if (newMove != null)
                {
                    if(playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumOfMoves)
                    {
                        playerUnit.Pokemon.LearnMove(newMove.Base);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name}learned {newMove.Base.Name}");
                        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
                    }
                    else
                    {
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name}trying to learn {newMove.Base.Name}");
                        yield return dialogBox.TypeDialog($"But it can't learn move than {PokemonBase.MaxNumOfMoves} moves");
                        yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);
                        yield return new WaitUntil(() => state != BattleStates.MoveToForget);
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);


            }

        }

        CheckForBattleOver(faintedUnit);
    }

    void CheckForBattleOver(BattleUnit faintedUnit )
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = PlayerParty.GetHealthyPokemon();
            if(nextPokemon != null)
            {
                openPartyScreen();
            }
            else
            {
                BattleOver(false);
            }
        }
        else
        {
            if (!IsTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextPokemon = TrainerParty.GetHealthyPokemon();
                if(nextPokemon != null)
                {
                    StartCoroutine(AboutToUse(nextPokemon));
                }
                else
                {
                    BattleOver(true);
                }
            }
           
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit!");

        if (damageDetails.DamageFactor > 1f)
            yield return dialogBox.TypeDialog("It's super effective!");
        else if(damageDetails.DamageFactor < 1f)
        {
            yield return dialogBox.TypeDialog("It's not very effective!");
        }
    }
    

    IEnumerator SetupBattle()
    {
        StateMachine = new StateMachine<BattleSystem>(this);

        playerUnit.Clear();
        enemyUnit.Clear();

        backgroundImage.sprite = (battleTrigger == BattleTrigger.LongGrass) ? grassBackground :waterBackground;

        if (!IsTrainerBattle)
        {
            playerUnit.SetUp(PlayerParty.GetHealthyPokemon());

            enemyUnit.SetUp(WildPokemon);

            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);

            yield return dialogBox.TypeDialog($" A wild {enemyUnit.Pokemon.Base.Name} appeared.");
        }
        else
        {
            //Trainer Battle
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);

            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogBox.TypeDialog($"{trainer.Name} wants to battle");

            // Send out first pokemon of the trainer
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyPokemon = TrainerParty.GetHealthyPokemon();
            enemyUnit.SetUp(enemyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {enemyPokemon.Base.Name}");

            // Send out first pokemon of the player
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = PlayerParty.GetHealthyPokemon();
            playerUnit.SetUp(playerPokemon);
            yield return dialogBox.TypeDialog($"Go {playerPokemon.Base.Name}!");
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
        }

        IsBattleOver = false;
        escapeAttempts = 0;

        partyScreen.Init();     

        StateMachine.ChangeState(ActionSelectionState.i);
    }

    void ActionSelection()
    {
        state = BattleStates.ActionSelection;
        dialogBox.SetDialog("Choose an action");
        dialogBox.EnableActionSelector(true);
    }

    IEnumerator RunMoveEffects(MoveEffects effects,Pokemon source,Pokemon target,MoveTarget moveTarget)
    {
 
        //强化相关
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                source.ApplyBoosts(effects.Boosts);
            }
            else
            {
                target.ApplyBoosts(effects.Boosts);
            }
        }

        //效果相关
        if(effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        //Volatile 效果相关
        print("VolatileStatus1"+effects.VolatileStatus);
        if (effects.VolatileStatus != ConditionID.none)
        {
            print("VolatileStatus2");
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    public void HandleUpdate()
    {
        StateMachine.Execute();

        if (state == BattleStates.PartyScreen)
        {
            HandlePartySelection();
        }else if(state == BattleStates.Bag){
            Action onBack = () =>
            {
                inventoryUI.gameObject.SetActive(false);
                state = BattleStates.ActionSelection;
            };

            Action<ItemBase> onItemUsed = (ItemBase usedItem) =>
            {
                  StartCoroutine(OnItemUsed(usedItem));         
            };

            //inventoryUI.HandleUpdate(onBack, onItemUsed);

        }else if(state == BattleStates.AboutToUse)
        {
            HandleAboutToUse();
        }
        else if (state == BattleStates.MoveToForget)
        {
            Action<int> onMoveSelected = (moveIndex) =>
            {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == PokemonBase.MaxNumOfMoves)
                {
                    //Do not learn the new move
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} did not learn {moveToLearn}"));
                }
                else
                {
                    var selectedMove = playerUnit.Pokemon.Moves[moveIndex].Base;
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learn {moveToLearn.Name}"));
                    //forget the select move and learn new move
                    Debug.Log(moveToLearn.Name);
                    playerUnit.Pokemon.Moves[moveIndex] = new Move(moveToLearn);
                }
                moveToLearn = null;
                state = BattleStates.RunningTurn;
            };
            //moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }

        /**
         * 
         
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(ThrowPokeball());
        }
        **/
    }

    bool CheckIfMoveHits(Move move,Pokemon source,Pokemon target)
    {
        if (move.Base.AlwaysHits)
        {
            return true;
        } 
        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
        {
            moveAccuracy *= boostValues[accuracy];
        }else
        {
            moveAccuracy /= boostValues[-accuracy];
        }

        if (evasion > 0)
        {
            moveAccuracy /= boostValues[evasion];
        }
        else
        {
            moveAccuracy *= boostValues[-evasion];
        }


        print("moveAccuracy" + moveAccuracy);
        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    void openPartyScreen()
    {
        //partyScreen.CalledFrom = state;
        state = BattleStates.PartyScreen;
        partyScreen.SetPartyData();
        partyScreen.gameObject.SetActive(true);
        
    }

    void OpenBag()
    {
        state = BattleStates.Bag;
        inventoryUI.gameObject.SetActive(true);

    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            ++currentAction;
        }else if (Input.GetKeyDown(KeyCode.A))
        {
             --currentAction;
        }else if (Input.GetKeyDown(KeyCode.W))
        {
            currentAction -= 2;
        }else if (Input.GetKeyDown(KeyCode.S))
        {
            currentAction += 2;
        }

        currentAction = Mathf.Clamp(currentAction, 0, 3);
        dialogBox.UpdateActionSelection(currentAction);

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (currentAction == 0)
            {
                //Fight
                MoveSelection();
            }else if (currentAction == 1)
            {
                //Bag
                OpenBag();
                //StartCoroutine(RunTurns(BattleAction.UseItem));
            }else if (currentAction == 2)
            {
                //Pokemon
                openPartyScreen();
            }else if (currentAction == 3)
            {
                //Run
                StartCoroutine(RunTurns(BattleAction.Run));
            }
        }
    }

    void HandleMoveSelection()
    {
       
 
        if (Input.GetKeyDown(KeyCode.D))
        {
            ++currentMove;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            --currentMove;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            currentMove -= 2;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentMove += 2;
        }

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);



        dialogBox.UpdateMoveSelection(currentMove,playerUnit.Pokemon.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.J))
        {
            var move = playerUnit.Pokemon.Moves[currentMove];
            if (move.PP == 0) return;


            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        }else if (Input.GetKeyDown(KeyCode.K))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }

    }

    

    void MoveSelection()
    {
        state = BattleStates.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleStates.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use { newPokemon.Base.Name}. Do you want to change pokemon?");
        state = BattleStates.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    void HandlePartySelection()
    {
        Action onSelected = () =>
        {
            var selectedMember = partyScreen.Selectedmember;
            if(selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted pokemon");
                return;
            }
            if(selectedMember == playerUnit.Pokemon)
            {
                partyScreen.SetMessageText("You can't switch with the pokemon");
                return;
            }

            partyScreen.gameObject.SetActive(false);
            
            //if (partyScreen.CalledFrom == BattleStates.ActionSelection)
            //{
            //   StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            //}
            //else
            //{
            //    state = BattleStates.Busy;
            //    bool isTrainerAboutToUse = partyScreen.CalledFrom == BattleStates.AboutToUse;
            //    StartCoroutine(SwitchPokemon(selectedMember, isTrainerAboutToUse));
            //}
            //partyScreen.CalledFrom = BattleStates.None;
        };

       

        Action onBack = () =>{
            if (playerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SetMessageText("You have to choose a pokemon to continue");
                return;
            }
            partyScreen.gameObject.SetActive(false);

            //if(partyScreen.CalledFrom == BattleStates.AboutToUse)
            //{
            //   
            //    StartCoroutine(SendNextTrainerPokemon());
            //}
            //else
            //{
            //    ActionSelection();
            //}
            //partyScreen.CalledFrom = BattleStates.None;
        };

        //partyScreen.HandleUpdate(onSelected,onBack);

  
    }

    IEnumerator ChooseMoveToForget(Pokemon pokemon,MoveBase newMove)
    {
        state = BattleStates.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you want to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveDate(pokemon.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleStates.MoveToForget;
    }

    public IEnumerator SwitchPokemon(Pokemon newPokemon)
    {
        if (playerUnit.Pokemon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back { playerUnit.Pokemon.Base.Name}");

            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }
        

        playerUnit.SetUp(newPokemon);
        dialogBox.SetMoveNames(newPokemon.Moves);
        yield return dialogBox.TypeDialog($"Go {newPokemon.Base.Name}!");

    }

    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleStates.Busy;

        var nextPokemon = TrainerParty.GetHealthyPokemon();
        enemyUnit.SetUp(nextPokemon);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextPokemon.Base.Name}!");

        state = BattleStates.RunningTurn;
    }

    IEnumerator OnItemUsed(ItemBase usedItem)
    {
        state = BattleStates.Busy;
        inventoryUI.gameObject.SetActive(false);

       if(usedItem is PokeballItem){
            yield return ThrowPokeball((PokeballItem)usedItem);
        }

        StartCoroutine(RunTurns(BattleAction.UseItem));
    }

    void HandleAboutToUse()
    {
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)){
            aboutToUseChoice = !aboutToUseChoice;
        }
        dialogBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKey(KeyCode.J))
        {
            dialogBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true)
            {
                //Yes
                openPartyScreen();
            }
            else
            {
                //No 
                StartCoroutine(SendNextTrainerPokemon());
            }
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator ThrowPokeball(PokeballItem pokeballItem)
    {
        state = BattleStates.Busy;

        if (IsTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't steal the trainers pokemons ");
        }

        yield return dialogBox.TypeDialog($"{player.Name} used {pokeballItem.Name.ToUpper()}!");
       
        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2,0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();
        pokeball.sprite = pokeballItem.Icon;

        yield return pokeball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 1), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return pokeball.transform.DOMoveY(enemyUnit.transform.position.y - 1.5f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchPokemon(enemyUnit.Pokemon, pokeballItem);

        for (int i = 0; i < Mathf.Min(shakeCount,3); i++)
        {
            yield return new WaitForSeconds(0.5f);
            yield return pokeball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }

        if(shakeCount == 4)
        {
            //pokemon is caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} was caught");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();

            PlayerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added your party");

            Destroy(pokeball);
            BattleOver(true);
        }
        else
        {
            //pokemon broke out
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if (shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free");
            else
                yield return dialogBox.TypeDialog($"almost caught it");

            Destroy(pokeball);
            state = BattleStates.RunningTurn;
        }
    }

    int TryToCatchPokemon(Pokemon pokemon, PokeballItem pokeballItem)
    {
        float rate = (3 * pokemon.MaxHP - 2 * pokemon.HP) * pokemon.Base.CatchRate * pokeballItem.CatchRateModifier * ConditionData.GetStatusBonus(pokemon.Status) / (3 * pokemon.MaxHP);
        if(rate >= 255)
        {
            return 4;
        }

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / rate));

        int shakeCount = 0;
        while(shakeCount < 4)
        {
            if(UnityEngine.Random.Range(0,65535)>= b)
                 break;

            ++shakeCount;
        }

        return shakeCount;
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleStates.RunningTurn;

        if(playerAction == BattleAction.Move)
        {
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
            {
                playerGoesFirst = false;
            }else if (enemyMovePriority == playerMovePriority)
            {
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;
            }
            //check who go first 
            
            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;
            //First turn 
            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleStates.BattleOver) yield break;

            if(secondPokemon.HP > 0)
            {
                //Second turn 
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleStates.BattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchPokemon)
            {
                var selectedPokemon = partyScreen.Selectedmember;
                state = BattleStates.Busy;
                yield return SwitchPokemon(selectedPokemon);
            }else if(playerAction == BattleAction.UseItem)
            {
                dialogBox.EnableActionSelector(false);
                //yield return ThrowPokeball();
            }else if (playerAction == BattleAction.Run)
            {

                //yield return TryToEacape();
            }

            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit,playerUnit,enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleStates.BattleOver) yield break;
        }

        if (state != BattleStates.BattleOver)
        {
            ActionSelection();
        }
    }

    public BattleDialogBox DialogBox => dialogBox;

    public BattleUnit PlayerUnit => playerUnit;

    public BattleUnit EnemyUnit => enemyUnit;

    public PartyScreen PartyScreen => partyScreen;

    public AudioClip BattleVictoryMusic => battleVictoryMusic;
}
