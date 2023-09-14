using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextSlot :MonoBehaviour, ISelectableItem
{
    [SerializeField] Text text;

    Color orginalColor;


    public void Init()
    {
        orginalColor = text.color;
    }

    public void Clear()
    {
        text.color = orginalColor;
    }

    public void OnSelectionChanged(bool selected)
    {
        text.color = (selected) ? GlobalSetting.i.HighlightedColor : orginalColor;
    }
    
    public void SetText(string s)
    {
        text.text = s;
    }
}
