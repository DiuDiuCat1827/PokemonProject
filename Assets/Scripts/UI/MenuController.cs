using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class MenuController : MonoBehaviour
{

    [SerializeField] GameObject menu;

    public event Action<int> onMenuSelected;
    public event Action onBack;

    List<Text> menuItems;

    int selectedItem = 0;

    public void Awake()
    {
       menuItems = menu.GetComponentsInChildren<Text>().ToList();
    }


    public void OpenMenu()
    {
        menu.SetActive(true);
        UpdateItemSelection();
    }

    public void CloseMenu()
    {
        menu.SetActive(false);
        UpdateItemSelection();
    }

    public void HandleUpdate()
    {
        int preSelection = selectedItem;

        if (Input.GetKeyDown(KeyCode.S))
        {
            selectedItem++;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            selectedItem--;
        }

        selectedItem = Mathf.Clamp(selectedItem, 0, menuItems.Count - 1);
        if (preSelection != selectedItem)
        {
            UpdateItemSelection();
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            onMenuSelected?.Invoke(selectedItem);
            CloseMenu();
        }else if (Input.GetKeyDown(KeyCode.K))
        {
            onBack?.Invoke();
            CloseMenu();
        }


    }

    void UpdateItemSelection()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
                if(i == selectedItem)
                {
                    menuItems[i].color = GlobalSetting.i.HighlightedColor;
                }
                else
                {
                    menuItems[i].color = Color.black;
                }
    
        }
    }
}
