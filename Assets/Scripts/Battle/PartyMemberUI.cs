using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hPBar;

    [SerializeField] Color highlightColor;

    Pokemon _pokemon;
    public void SetData(Pokemon pokemon)
    {
        _pokemon = pokemon;
        nameText.text = pokemon.Base.Name;
        levelText.text = "Lv" + pokemon.Level;
        hPBar.SetHP((float)pokemon.HP / pokemon.MaxHP);

    }


    public IEnumerator UpdateHp()
    {
        yield return hPBar.SetHPSmooth((float)_pokemon.HP / _pokemon.MaxHP);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            nameText.color = highlightColor;
        }
        else
        {
            nameText.color = Color.black;
        }
    }
}
