using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;
using GDE.GenericSelectionUI;

public enum ItemCategory { Items,Pokeballs,Tms,}

public enum InventoryUIState  { ItemSelection, PartySelection, MoveToForget, Busy}

public class InventoryUI : SelectionUI<TextSlot>
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

        SetItems(slotUIList.Select(s => s.GetComponent<TextSlot>()).ToList());

        UpdateSelectionUI();
    }

    public override void HandleUpdate()
    {
        int prevCategory = selectedCategory;

        if (Input.GetKeyDown(KeyCode.D))
        {
            selectedCategory++;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            selectedCategory--;
        }
        

        if (selectedCategory > Inventory.ItemCategories.Count - 1)
        {
            selectedCategory = 0;
        }
        else if (selectedCategory < 0)
        {
            selectedCategory = Inventory.ItemCategories.Count - 1;
        }

       

        if (prevCategory != selectedCategory)
        {
            ResetSelection();
            categoryText.text = Inventory.ItemCategories[selectedCategory];
            UpdateItemList();
        }
      

        base.HandleUpdate();
    }


    IEnumerator ItemSelected()
    {
        inventoryState = InventoryUIState.Busy;
        var item = inventory.GetItem(selectedItem, selectedCategory);


        if(GameController.Instance.State == GameState.Shop)
        {
            onItemUsed.Invoke(item);
            inventoryState = InventoryUIState.ItemSelection;
            yield break;
        }

        if(GameController.Instance.State == GameState.Battle)
        {
            //InBattle
            if (!item.CanUseInBattle)
            {
                yield return DialogManager.Instance.ShowDialogText($"This item cannot be used in battle");
                inventoryState = InventoryUIState.ItemSelection;
                yield break;
            }
        }
        else
        {
            //OutSizeBattle
            if (!item.CanUseOutsideBattle)
            {
                yield return DialogManager.Instance.ShowDialogText($"This item cannot be used outside battle");
                inventoryState = InventoryUIState.ItemSelection;
                yield break;
            }
        }

        if(selectedCategory == (int) ItemCategory.Pokeballs)
        {
            StartCoroutine(UseItem());
        }
        else
        {
            OpenPartyScreen();

            if(item is TmItem)
            {
                partyScreen.ShowIfTmIsUsable(item as TmItem);
            }
        }
    }

    IEnumerator UseItem()
    {

        inventoryState = InventoryUIState.Busy;

        yield return HandleTmItems();

        var item = inventory.GetItem(selectedItem, selectedCategory);
        var pokemon = partyScreen.Selectedmember;

        // handle Evolution Items
        if(item is EvolutionItem)
        {
            var evolution = pokemon.CheckForEvolution(item);
            if(evolution != null)
            {
                yield return  EvolutionManager.i.Evolve(pokemon, evolution);
            }
            else
            {
                yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
                ClosePartyScreen();
                yield break;
            }
        }

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
            Debug.Log(selectedCategory);
            if(selectedCategory == (int)ItemCategory.Items)
            {
                yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
            }
          
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
        if (pokemon.HasMove(tmItem.Move))
        {
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} already learned {tmItem.Move.Name}!");
            yield break;
        }

        if (!tmItem.CanBeTaught(pokemon))
        {
            yield return DialogManager.Instance.ShowDialogText($"{pokemon.Base.Name} can't learned {tmItem.Move.Name}!");
            yield break;
        }

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

        partyScreen.ClearMemberSlotMessages();
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

    public override void UpdateSelectionUI()
    {
        base.UpdateSelectionUI();

        var slots = inventory.GetSlotsByCategory(selectedCategory);

        if (slots.Count > 0){
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

