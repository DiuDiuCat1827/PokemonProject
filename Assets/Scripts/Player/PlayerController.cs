using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour, ISavable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;

    public float moveSpeed;

    private Vector2 input;

    public static PlayerController i { get;private set;}

    // Start is called before the first frame update

    private Character character;
    private void Awake()
    {
        i = this;
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
           StartCoroutine( Interact());
        }
    }

    IEnumerator Interact()
    {
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        //Debug.DrawLine(transform.position, interactPos, Color.green, 0.5f);
        var collider = Physics2D.OverlapCircle(interactPos, 0.3f,GameLayers.i.InteractableLayer | GameLayers.i.WaterLayer);

        if (collider != null)
        {
            yield return collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    void Run(Vector3 targetPos)
    {
        if ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }
    }

    public Character Character => character;


    private bool IsWalkable(Vector3 targetPos)
    {
        if (Physics2D.OverlapCircle(targetPos,0.3f,GameLayers.i.SolidLayer| GameLayers.i.InteractableLayer) !=null )
        {
            return false;
        }
        return true;
    }

    IPlayerTrigger currentlyInTrigger;

    private void OnMoveOver()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, character.offsetY), 0.2f, GameLayers.i.TriggerableLayers);
        
        IPlayerTrigger triggerable = null;
        foreach(var collider in colliders)
        {
            triggerable = collider.GetComponent<IPlayerTrigger>();
            if (triggerable != null)
            {
                if(triggerable == currentlyInTrigger && !triggerable.TriggerRepeatedly)
                {
                    break;
                 }
                   
                triggerable.OnPlayerTriggered(this);
                currentlyInTrigger = triggerable;
                break;
            }
        }
        //CheckForEncounters();
        //CheckIfInTrainerView();
        if(colliders.Count() == 0 || triggerable != currentlyInTrigger)
        {
            currentlyInTrigger = null;
        }
    }

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }

    public object CaptureState()
    {
        var saveData = new PlayerSaveDate()
        {
            position = new float[] {transform.position.x,transform.position.y} ,
            pokemons = GetComponent<PokemonParty>().Pokemons.Select( p => p.GetSaveData()).ToList()
        };
      
        return saveData;
    }

    public void RestoreState(object state)
    {
        var saveData = (PlayerSaveDate)state;
        var pos = saveData.position;
        transform.position = new Vector3(pos[0], pos[1]);

        //Restore Party
        GetComponent<PokemonParty>().Pokemons = saveData.pokemons.Select(s => new Pokemon(s)).ToList();
        
    }
}

[Serializable]
public class PlayerSaveDate
{
    public float[] position;
    public List<PokemonSaveData> pokemons;
}
