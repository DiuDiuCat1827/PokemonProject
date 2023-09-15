using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerControler : MonoBehaviour,Interactable,ISavable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject fov;

    [SerializeField] AudioClip trainerAppearsClip;

    bool bBattleLost = false;
    Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        SetFovRotation(character.Animator.GefaultDirection);
    }

    private void Update()
    {
        character.HandleUpdate();
    }

    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        GameController.Instance.StateMachine.Push(CutsceneState.i);
        AudioManager.i.PlayMusic(trainerAppearsClip);

        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));


        yield return character.Move(moveVec);

        // show dialog

        yield return  DialogManager.Instance.ShowDialog(dialog);

        GameController.Instance.StateMachine.Pop();

        GameController.Instance.StartTrainerBattle(this);
    }

    public void SetFovRotation(FacingDirection dir)
    {
        float angle = 0f;
        if (dir == FacingDirection.Up)
            angle = 90f;
        else if (dir == FacingDirection.Left)
            angle = 180f;
        else if (dir == FacingDirection.Down)
            angle = 270f;

        fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    public IEnumerator Interact(Transform initiator)
    {
        
        character.LookTowards(initiator.position);


        if (!bBattleLost) {

            AudioManager.i.PlayMusic(trainerAppearsClip);
            yield return DialogManager.Instance.ShowDialog(dialog);
           
            GameController.Instance.StartTrainerBattle(this);

        }
        else
        {
            yield return  DialogManager.Instance.ShowDialog(dialogAfterBattle);
        }

        
    }

    public void BattleLost()
    {
        bBattleLost = true;
        fov.gameObject.SetActive(false);
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
        return bBattleLost;
    }

    public void RestoreState(object state)
    {
        bBattleLost = (bool) state;
        if (bBattleLost)
        {
            fov.gameObject.SetActive(false); 
        }
    }
}
