using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;

public enum ItemCategory { Items,Pokeballs,Tms,}

public enum InventoryUIState  { ItemSelection, PartySelection, MoveToForget, Busy}

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
    [SerializeField] MoveSelectionUI moveSelectionUI;



    Action<ItemBase> onItemUsed;

    int selectedItem = 0;
    int selectedCategory = 0;
    MoveBase moveToLearn;

    InventoryUIState inventoryState;

    const int itemsInViewport = 8;

    List<ItemSlotUI> slotUIList;
    Inventory inventory;
    RectTransform itemListRect;
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



    public void HandleUpdate(Action onBack,Action<ItemBase> onItemUsed = null)
    {
        this.onItemUsed = onItemUsed;
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
                ItemSelected();
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
        }else if(inventoryState == InventoryUIState.MoveToForget)
        {
            Action<int> onMoveSelected = (int moveIndex) =>
            {
                StartCoroutine(OnMoveToForgetSelected(moveIndex));
            };
            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }

    void ItemSelected()
    {
        if(selectedCategory == (int) ItemCategory.Pokeballs)
        {
            StartCoroutine(UseItem());
        }
        else
        {
            OpenPartyScreen();
        }
    }

    IEnumerator UseItem()
    {

        inventoryState = InventoryUIState.Busy;

        yield return HandleTmItems();

        var useItem =  inventory.UseItem(selectedItem, partyScreen.Selectedmember, selectedCategory);
        if(useItem != null)
        {
            if (useItem is RecoveryItem)
            {
                yield return DialogManager.Instance.ShowDialogText($"The Player used {useItem.Name}");
            }
           

            onItemUsed?.Invoke(useItem);
        }
        else
        {
           yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
        }

        ClosePartyScreen();
    }

    IEnumerator HandleTmItems()
    {
       var tmItem =  inventory.GetItem(selectedItem, selectedCategory) as TmItem;

       if(tmItem == null)
        {
            yield break;
        }

        var pokemon = partyScreen.Selectedmember;
        if(pokemon.Moves.Count < PokemonBase.MaxNumOfMoves)
        {
            pokemon.LearnMove(tmItem.Move);
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} learned {tmItem.Move.Name}!");
        }
        else
        {
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} is trying to learn {tmItem.Move.Name}!");
            yield return DialogManager.Instance.ShowDialogText($"But it cannot learn more than {PokemonBase.MaxNumOfMoves} moves");
            yield return ChooseMoveToForget(pokemon, tmItem.Move);
            yield return new WaitUntil(() => inventoryState != InventoryUIState.MoveToForget);
        }
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

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove)
    {
        inventoryState = InventoryUIState.Busy;
        yield return DialogManager.Instance.ShowDialogText($"Choose a move you want to forget", true, false);
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveDate(pokemon.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        inventoryState = InventoryUIState.MoveToForget;
    }

    void UpdateItemSelection()
    {
        var slots = inventory.GetSlotsByCategory(selectedCategory);
        selectedItem = Mathf.Clamp(selectedItem, 0, slots.Count - 1);


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

    IEnumerator OnMoveToForgetSelected(int moveIndex)
    {
        var pokemon = partyScreen.Selectedmember;

        DialogManager.Instance.CloseDialog();

        moveSelectionUI.gameObject.SetActive(false);

        if (moveIndex == PokemonBase.MaxNumOfMoves)
        {
            //Do not learn the new move
            yield return StartCoroutine(DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} did not learn {moveToLearn}"));
        }
        else
        {
            //Forget the selected move and learn new move 
            var selectedMove = pokemon.Moves[moveIndex].Base;
            yield return StartCoroutine(DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} forgot {selectedMove.Name} and learn {moveToLearn.Name}"));
            //forget the select move and learn new move
            pokemon.Moves[moveIndex] = new Move(moveToLearn);
        }
        moveToLearn = null;
        inventoryState = InventoryUIState.ItemSelection;
    }
}

