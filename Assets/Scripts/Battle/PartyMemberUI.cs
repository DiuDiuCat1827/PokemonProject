using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hPBar;

    Pokemon _pokemon;
    public void Init(Pokemon pokemon)
    {
        _pokemon = pokemon;
        UpdateData();

        _pokemon.OnHPChanged += UpdateData;
    }

    void UpdateData()
    {
         nameText.text = _pokemon.Base.Name;
        levelText.text = "Lv" + _pokemon.Level;
        hPBar.SetHP((float)_pokemon.HP / _pokemon.MaxHP);
    }


    public IEnumerator UpdateHp()
    {
        yield return hPBar.SetHPSmooth((float)_pokemon.HP / _pokemon.MaxHP);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            nameText.color = GlobalSetting.i.HighlightedColor;
        }
        else
        {
            nameText.color = Color.black;
        }
    }
}
