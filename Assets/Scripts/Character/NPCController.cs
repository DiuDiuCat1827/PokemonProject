using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, Interactable
{
    [SerializeField] Dialog dialog;
    [SerializeField] QuestBase questToStart;
    [SerializeField] List<Sprite> sprites;
    [SerializeField] List<Vector2> movementPattern;
    [SerializeField] float timeBetweenPattern;



    SpriteAnimator spriteAnimator;
   
    Character character;

    ItemGiver itemGiver;

    NPCState state;
    float idleTimer = 0f;
    int currentPattern = 0;

    Quest activeQuest;
    private void Start()
    {
        //spriteAnimator = new SpriteAnimator(sprites, GetComponent<SpriteRenderer>());
        //spriteAnimator.Start();
        character = GetComponent<Character>();
        itemGiver = GetComponent<ItemGiver>();
    }

    private void Update()
    {
 

        if (state == NPCState.Idle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer > timeBetweenPattern)
            {
                idleTimer = 0f;
                if(movementPattern.Count > 0)
                {
                    StartCoroutine(Walk());
                }
               
            }
        }
        character.HandleUpdate();
    }

    IEnumerator Walk()
    {
        state = NPCState.Waking;

        var oldPos = transform.position;

        yield return character.Move(movementPattern[currentPattern]);

        if (transform.position != oldPos)
        {
            currentPattern = (currentPattern + 1) % movementPattern.Count;
        }

    
        state = NPCState.Idle;

    }

    public IEnumerator Interact(Transform initiator)
    {
        if(state == NPCState.Idle)
        {
            state = NPCState.Dialog;
            character.LookTowards(initiator.position);

            if(itemGiver != null && itemGiver.CanBeGiven())
            {
               yield return  itemGiver.GiveItem(initiator.GetComponent<PlayerController>());
            }else if(questToStart != null)
            {
                activeQuest = new Quest(questToStart);
                yield return activeQuest.StartQuest();
                questToStart = null;
            }else if(activeQuest != null)
            {
                if (activeQuest.CanBeCompleted())
                {
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                }
                else
                {
                    yield return DialogManager.Instance.ShowDialog(activeQuest.Base.InProgressDialogue);
                }
            }
            else
            {
                yield return DialogManager.Instance.ShowDialog(dialog);
            }

            
            idleTimer = 0f;
            state = NPCState.Idle;         
        }
      
    }

}

public enum NPCState { Idle,Waking,Dialog }
