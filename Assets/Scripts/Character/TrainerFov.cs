using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerFov : MonoBehaviour, IPlayerTrigger
{
    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        GameController.Instance.OnEnterTrainerView(GetComponentInParent<TrainerControler>());
    }
}
