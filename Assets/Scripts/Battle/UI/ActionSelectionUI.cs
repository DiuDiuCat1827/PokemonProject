using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GDE.GenericSelectionUI;
using System.Linq;

public class ActionSelectionUI : SelectionUI<TextSlot>
{
    private void Start()
    {
        SetSelectionSetting(SelectionType.Grid, 2);

        SetItems(GetComponentsInChildren<TextSlot>().ToList());
    }
}
