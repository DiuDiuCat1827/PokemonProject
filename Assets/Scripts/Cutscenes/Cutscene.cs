using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Cutscene : MonoBehaviour, IPlayerTrigger
{
    [SerializeReference]
    [SerializeField] List<CutsceneAction> actions;

    public bool TriggerRepeatedly => false;

    public IEnumerator Play()
    {
        GameController.Instance.StateMachine.Push(CutsceneState.i);
        var i = 1;
        foreach(var action in actions)
        {
            if (action.WaitForCompletion)
            {
                yield return action.Play();
            }
            else
            {
                StartCoroutine(action.Play());
            }
            
        }

        GameController.Instance.StateMachine.Pop();
    }

    public void AddAction(CutsceneAction action)
    {
#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(this, "Add action to cutscene.");
#endif

        action.Name = action.GetType().ToString();
        actions.Add(action);
    }



    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        StartCoroutine(Play());
    }
}
