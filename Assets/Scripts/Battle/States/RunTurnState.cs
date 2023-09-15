using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDEUtils.StateMachine;

public class RunTurnState : State<BattleSystem>
{
    public static RunTurnState i { get; private set; }

    private void Awake()
    {
        i = this;
    }

    BattleUnit playerUnit;
    BattleUnit enemyUnit;
    BattleDialogBox dialogBox;
    PartyScreen partyScreen;
    bool isTrainerBattle;
    PokemonParty playerParty;
    PokemonParty trainerParty;


    BattleSystem battleSystem;

    public override void Enter(BattleSystem owner)
    {
        battleSystem = owner;
        playerUnit = battleSystem.PlayerUnit;
        enemyUnit = battleSystem.EnemyUnit;
        dialogBox = battleSystem.DialogBox;
        partyScreen = battleSystem.PartyScreen;
        isTrainerBattle = battleSystem.IsTrainerBattle;
        playerParty = battleSystem.PlayerParty;
        trainerParty = battleSystem.TrainerParty;

        StartCoroutine(RunTurns(battleSystem.SelectedAction));
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[battleSystem.SelectedMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
            {
                playerGoesFirst = false;
            }
            else if (enemyMovePriority == playerMovePriority)
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
            if (battleSystem.IsBattleOver) yield break;

            if (secondPokemon.HP > 0)
            {
                //Second turn 
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (battleSystem.IsBattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchPokemon)
            {         
                yield return battleSystem.SwitchPokemon(battleSystem.SelectedPokemon);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                dialogBox.EnableActionSelector(false);
                //yield return ThrowPokeball();
            }
            else if (playerAction == BattleAction.Run)
            {

                yield return TryToEacape();
            }

            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (battleSystem.IsBattleOver) yield break;
        }

        if (!battleSystem.IsBattleOver)
        {
            battleSystem.StateMachine.ChangeState(ActionSelectionState.i);
        }
    }

    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
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
        }
        else
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


    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
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
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.WaitForHPUpdate();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                    {
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
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

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
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
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        //Volatile 效果相关
        print("VolatileStatus1" + effects.VolatileStatus);
        if (effects.VolatileStatus != ConditionID.none)
        {
            print("VolatileStatus2");
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator TryToEacape()
    {

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles!");
            yield break;
        }

        ++battleSystem.escapeAttempts;

        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely!");
            battleSystem.BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * battleSystem.escapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely!");
                battleSystem.BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Eacape fail!");
            }
        }
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{ faintedUnit.Pokemon.Base.Name} Fainted");
        faintedUnit.PlayFaintAnimation();

        yield return new WaitForSeconds(2f);

        if (!faintedUnit.IsPlayerUnit)
        {
            bool battleWon = true;
            if (battleSystem.IsTrainerBattle)
            {
                battleWon = trainerParty.GetHealthyPokemon() == null;
            }
            if (battleWon)
            {
                AudioManager.i.PlayMusic(battleSystem.BattleVictoryMusic, fade: true);
            }

            int expYield = faintedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;

            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;

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
                    if (playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumOfMoves)
                    {
                        playerUnit.Pokemon.LearnMove(newMove.Base);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name}learned {newMove.Base.Name}");
                        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
                    }
                    else
                    {
                        //yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name}trying to learn {newMove.Base.Name}");
                        //yield return dialogBox.TypeDialog($"But it can't learn move than {PokemonBase.MaxNumOfMoves} moves");
                        //yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);
                        //yield return new WaitUntil(() => state != BattleStates.MoveToForget);
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);


            }

        }

       yield return CheckForBattleOver(faintedUnit);
    }

    IEnumerator CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = playerParty.GetHealthyPokemon();
            if (nextPokemon != null)
            {
                AboutToUseState.i.NewPokemon = nextPokemon;
                yield return battleSystem.StateMachine.PushAndWait(AboutToUseState.i);
            }
            else
            {
                battleSystem.BattleOver(false);
            }
        }
        else
        {
            if (!isTrainerBattle)
            {
                battleSystem.BattleOver(true);
            }
            else
            {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if (nextPokemon != null)
                {
                    AboutToUseState.i.NewPokemon = nextPokemon;
                    yield return battleSystem.StateMachine.PushAndWait(AboutToUseState.i);
                }
                else
                {
                    battleSystem.BattleOver(true);
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
        else if (damageDetails.DamageFactor < 1f)
        {
            yield return dialogBox.TypeDialog("It's not very effective!");
        }
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (battleSystem.IsBattleOver) yield break;

        //Status like burn or psn will hurt pokemon after the turn 
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return HandlePokemonFainted(sourceUnit);

        }
    }
}
