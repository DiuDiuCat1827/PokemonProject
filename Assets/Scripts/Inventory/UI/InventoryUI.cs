using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public enum InventoryUIState  { ItemSelection, PartySelection,Busy}

public class InventoryUI : MonoBehaviour
{
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;


    [SerializeField] Text categoryText;
    [SerializeField] Image itemIcon;
    [SerializeField] Text itemDescription;

    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    [SerializeField] PartyScreen partyScreen;

    List<ItemSlotUI> slotUIList;
    RectTransform itemListRect;

    int selectedItem = 0;
    int selectedCategory = 0;

    InventoryUIState inventoryState;

    const int itemsInViewport = 8;

    Inventory inventory;

    private void Awake()
    {
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateItemList();

        inventory.OnUpdated += UpdateItemList;
    }

    void UpdateItemList()
    {
        //Clear all the exist items
        foreach (Transform child in itemList.transform)
        {
            Destroy(child.gameObject);
        }

        slotUIList = new List<ItemSlotUI>();

        foreach (var itemSlot in inventory.GetSlotsByCategory(selectedCategory))
        {
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetData(itemSlot);

            slotUIList.Add(slotUIObj);
        }

        UpdateItemSelection();
    }



    public void HandleUpdate(Action onBack)
    {

        if (inventoryState == InventoryUIState.ItemSelection)
        {
            int preSelection = selectedItem;
            int prevCategory = selectedCategory;

            if (Input.GetKeyDown(KeyCode.S))
            {
                selectedItem++;
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                selectedItem--;
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                selectedCategory++;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                selectedCategory--;
            }
  
            if(selectedCategory > Inventory.ItemCategories.Count - 1)
            {
                selectedCategory = 0;
            }else if(selectedCategory < 0)
            {
                selectedCategory = Inventory.ItemCategories.Count - 1;
            }

            selectedItem = Mathf.Clamp(selectedItem, 0, inventory.GetSlotsByCategory(selectedCategory).Count - 1);

            if (prevCategory != selectedCategory)
            {
                ResetSelection();
                categoryText.text = Inventory.ItemCategories[selectedCategory];
                UpdateItemList();
            }else if (preSelection != selectedItem)
            {
                UpdateItemSelection();
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                OpenPartyScreen();
            }else if (Input.GetKeyDown(KeyCode.K))
            {
                onBack?.Invoke();
            }
        }else if(inventoryState == InventoryUIState.PartySelection)
        {
            //Handle party selection
            Action onSelected = () =>
             {
                 StartCoroutine(UseItem());
            };

            Action onBackPartyScreen = () =>
            {
                ClosePartyScreen();
            };

            partyScreen.HandleUpdate(onSelected, onBackPartyScreen);
        }
    }

    IEnumerator UseItem()
    {

        inventoryState = InventoryUIState.Busy;
        var useItem =  inventory.UseItem(selectedItem, partyScreen.Selectedmember);
        if(useItem != null)
        {
           yield return DialogManager.Instance.ShowDialogText($"The Player used {useItem.Name}");
        }
        else
        {
           yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
        }

        ClosePartyScreen();
    }

    void ResetSelection()
    {
        selectedItem = 0;
        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);
        itemIcon.sprite = null;
        itemDescription.text = "";
    }

    void OpenPartyScreen()
    {
        inventoryState = InventoryUIState.PartySelection;
        partyScreen.SetPartyData();
        partyScreen.gameObject.SetActive(true);
    }

    void ClosePartyScreen()
    {
        inventoryState = InventoryUIState.ItemSelection;
        partyScreen.gameObject.SetActive(false);
    }

    void UpdateItemSelection()
    {
        var slots = inventory.GetSlotsByCategory(selectedCategory);

        for (int i = 0; i < slotUIList.Count; i++)
        {
            if (i == selectedItem)
            {
                slotUIList[i].NameText.color = GlobalSetting.i.HighlightedColor;
            }
            else
            {
                slotUIList[i].NameText.color = Color.black;
            }

        }



        selectedItem = Mathf.Clamp(selectedItem, 0, slots.Count -1);

        if (slots.Count > 0)
        {
            var item = slots[selectedItem].Item;
            itemIcon.sprite = item.Icon;
            itemDescription.text = item.Description;
        }
        
        

        HandleScrolling();
    }

    void HandleScrolling()
    {
        if (slotUIList.Count <= itemsInViewport) return;

        float scrollPos = Mathf.Clamp(selectedItem - itemsInViewport/2,0,selectedItem) * slotUIList[0].Height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedItem + itemsInViewport / 2 < slotUIList.Count;
        downArrow.gameObject.SetActive(showDownArrow);

    }
}

