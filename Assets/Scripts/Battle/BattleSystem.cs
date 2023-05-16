using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy ,PartyScreen , BattleOver,None}
public enum BattleAction { Move,SwitchPokemon,UseItem,Run}

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;

    [SerializeField] BattleUnit enemyUnit;

    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;


    public event Action<bool> OnBattleOver;

    BattleState state;
    BattleState prevState;

    int currentAction;
    int currentMove;
    int currentMember;

    PokemonParty playerParty;
    Pokemon wildPokemon;

    public void StartBattle(PokemonParty playerParty,Pokemon wildPokemon)
    {
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;
        StartCoroutine(SetupBattle());
    }

   
    void BattleOver(bool win)
    {
        state = BattleState.BattleOver;
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver(win);
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

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
            if (state == BattleState.BattleOver) yield break;

            if(secondPokemon.HP > 0)
            {
                //Second turn 
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchPokemon)
            {
                var selectedPokemon = playerParty.Pokemons[currentMember];
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedPokemon);
            }

            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit,playerUnit,enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }

        if (state != BattleState.BattleOver)
        {
            ActionSelection();
        }
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
            yield return sourceUnit.Hud.UpdateHp();
            yield break;
            
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {
          sourceUnit.PlayAttackAnimation();
          targetUnit.PlayHitAnimation();

          if (move.Base.Category == MoveCategory.Status)
          {
            yield return RunMoveEffects(move.Base.Effects,sourceUnit.Pokemon,targetUnit.Pokemon,move.Base.Target);
          }
          else
          {
            var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
            yield return targetUnit.Hud.UpdateHp();
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
            yield return dialogBox.TypeDialog($"{ targetUnit.Pokemon.Base.Name}Fainted");
            targetUnit.PlayFaintAnimation();

            yield return new WaitForSeconds(2f);
            CheckForBattleOver(targetUnit);
          }
        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed");
        }

 
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(()=> state == BattleState.RunningTurn);

        //Status like burn or psn will hurt pokemon after the turn 
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.UpdateHp();


        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return dialogBox.TypeDialog($"{ sourceUnit.Pokemon.Base.Name}Fainted");
            sourceUnit.PlayFaintAnimation();

            yield return new WaitForSeconds(2f);
            CheckForBattleOver(sourceUnit);
        }
    }

    void CheckForBattleOver(BattleUnit faintedUnit )
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = playerParty.GetHealthyPokemon();
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
            BattleOver(true);
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
        playerUnit.SetUp(playerParty.GetHealthyPokemon());
      
        enemyUnit.SetUp(wildPokemon);
     
        partyScreen.Init();

        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);

        yield return  dialogBox.TypeDialog($" A wild {enemyUnit.Pokemon.Base.Name} appeared.");

        ActionSelection();
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
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
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }else if(state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
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
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Pokemons);
        partyScreen.gameObject.SetActive(true);
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
            }else if (currentAction == 2)
            {
                //Pokemon
                prevState = state;
                openPartyScreen();
            }else if (currentAction == 3)
            {
                //Run
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
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            ++currentMember;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            --currentMember;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            currentMember -= 2;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentMember += 2;
        }

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);


        if (Input.GetKeyDown(KeyCode.J))
        {
            var selectedMember = playerParty.Pokemons[currentMember];
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

            if (prevState == BattleState.ActionSelection)
            {
                prevState = BattleState.None;
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchPokemon(selectedMember));
            }

            state = BattleState.Busy;
            StartCoroutine(SwitchPokemon(selectedMember));
        }else if (Input.GetKeyDown(KeyCode.K))
        {
            partyScreen.gameObject.SetActive(false);
            ActionSelection();
        }
    }
    IEnumerator SwitchPokemon(Pokemon newPokemon)
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


        state = BattleState.RunningTurn;
    }
}
