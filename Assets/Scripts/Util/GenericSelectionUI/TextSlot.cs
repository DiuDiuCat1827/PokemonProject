using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextSlot :MonoBehaviour, ISelectableItem
{
    [SerializeField] Text text;

    Color orginalColor;

    private void Awake()
    {
        orginalColor = text.color;
    }

    public void OnSelectionChanged(bool selected)
    {
        text.color = (selected) ? GlobalSetting.i.HighlightedColor : orginalColor;


    }
    
}
