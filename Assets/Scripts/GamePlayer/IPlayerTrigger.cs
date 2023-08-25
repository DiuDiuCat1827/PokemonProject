using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerTrigger 
{
    void OnPlayerTriggered(PlayerController player);

    bool TriggerRepeatedly { get; }
}
