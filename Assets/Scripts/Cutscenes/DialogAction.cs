using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogAction : CutsceneAction
{
    [SerializeField] Dialog dialog;

    public override IEnumerator Play()
    {
        Debug.Log("DialogAction");
        yield return DialogManager.Instance.ShowDialog(dialog);
    }
}
