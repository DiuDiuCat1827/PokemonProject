
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Inventory : MonoBehaviour
{
    [SerializeField] List<ItemSlot> slots;
    [SerializeField] List<ItemSlot> pokeballSlots;
    [SerializeField] List<ItemSlot> tmSlots;

    List<List<ItemSlot>> allSlots;

    public event Action OnUpdated;

    public List<ItemSlot> Slots => slots;

    private void Awake()
    {
        allSlots = new List<List<ItemSlot>>() { slots, pokeballSlots, tmSlots };
    }

    public static List<string> ItemCategories { get;set; } = new List<string>()
    {
        "ITEMS","POKEBALLS","TMs & HMs"
    };

    public List<ItemSlot> GetSlotsByCategory(int categoryIndex)
    {
        return allSlots[categoryIndex];
    }

    public ItemBase GetItem(int itemIndex, int selectedCategory)
    {
        var currentSlots = GetSlotsByCategory(selectedCategory);

        return currentSlots[itemIndex].Item;
    }

    public ItemBase  UseItem(int itemIndex,Pokemon selectedPokemon, int selectedCategory)
    {
        var item = GetItem(itemIndex, selectedCategory);
        bool itemUsed = item.Use(selectedPokemon);
        if (itemUsed)
        {
            if (!item.IsReusable)
            {
                RemoveItem(item, selectedCategory);
            }
            
            return item;
        }

        return null;
    }

    public void RemoveItem(ItemBase item, int category)
    {
        var currentSlots = GetSlotsByCategory(category);

        var itemSlot = currentSlots.First(slots => slots.Item == item);
        itemSlot.Count--;
        if (itemSlot.Count == 0)
        {
            currentSlots.Remove(itemSlot);
        }

        OnUpdated?.Invoke();
    }

    public static Inventory GetInventory()
    {
       return FindObjectOfType<PlayerController>().GetComponent<Inventory>();
    }
}


[Serializable]
public class ItemSlot
{
    [SerializeField]  ItemBase item;
    [SerializeField]  int count;

    public ItemBase Item => item;

    public int Count {
        get => count;
        set => count = value;
    }
}
