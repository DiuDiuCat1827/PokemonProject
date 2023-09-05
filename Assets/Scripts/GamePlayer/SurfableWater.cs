using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;

public class SurfableWater :MonoBehaviour, Interactable, IPlayerTrigger
{
    public bool TriggerRepeatedly => true;

    bool isJumpingToWater = false;

    public void OnPlayerTriggered(PlayerController player)
    {
        if (UnityEngine.Random.Range(1, 101) <= 10)
        {
           
            GameController.Instance.StartBattle(BattleTrigger.Water);
        }
    }

    public IEnumerator Interact(Transform initiator)
    {
        var animator = initiator.GetComponent<CharacterAnimator>();
        if (animator.IsSurfing | isJumpingToWater)
        {
            yield break;
        }

        yield return DialogManager.Instance.ShowDialogText("This water is deep blue!");

        var pokemonWithSurf = initiator.GetComponent<PokemonParty>().Pokemons.FirstOrDefault(p => p.Moves.Any(m => m.Base.Name == "Surf"));

        if (pokemonWithSurf != null)
        {
            int selectedChoice = 0;
            yield return DialogManager.Instance.ShowDialogText($"Should {pokemonWithSurf.Base.Name} use surf?",
                choices: new List<string>() { "Yes", "No" },
                onChoiceSelected: (selection) => selectedChoice = selection);

            if (selectedChoice == 0)
            {
                yield return DialogManager.Instance.ShowDialogText($"{pokemonWithSurf.Base.Name} used surf!");
                
                var dir = new Vector3(animator.MoveX, animator.MoveY);
                var targetPos = initiator.position + dir;

                isJumpingToWater = true;
                yield return initiator.DOJump(targetPos, 0.3f, 1, 0.5f).WaitForCompletion();
                isJumpingToWater = false;
                animator.IsSurfing = true;
            }
        }
    }
}
