using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;

    const float offsetY = 0.3f;
    public float moveSpeed;

    private Vector2 input;

    // Start is called before the first frame update

    private Character character;
    private void Awake()
    {
        character = GetComponent<Character>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    public void HandleUpdate()
    {
        if (!character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0) input.y = 0;
            if (input.y != 0) input.x = 0;
            if (input != Vector2.zero) 
            {

               StartCoroutine(character.Move(input,OnMoveOver));
                
            }
        }

        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.J))
        {
            Interact();
        }
    }

    void Interact()
    {
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        //Debug.DrawLine(transform.position, interactPos, Color.green, 0.5f);
        var collider = Physics2D.OverlapCircle(interactPos, 0.3f,GameLayers.i.InteractableLayer);

        if (collider != null)
        {
            collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    void Run(Vector3 targetPos)
    {
        if ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }
    }


    private bool IsWalkable(Vector3 targetPos)
    {
        if (Physics2D.OverlapCircle(targetPos,0.3f,GameLayers.i.SolidLayer| GameLayers.i.InteractableLayer) !=null )
        {
            return false;
        }
        return true;
    }

    private void OnMoveOver()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, offsetY), 0.2f, GameLayers.i.TriggerableLayers);
        foreach(var collider in colliders)
        {
            var triggerable = collider.GetComponent<IPlayerTrigger>();
            if (triggerable != null)
            {
                character.Animator.IsMoving = false;
                triggerable.OnPlayerTriggered(this);
                break;
            }
        }
        //CheckForEncounters();
        //CheckIfInTrainerView();
    }

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }
}
