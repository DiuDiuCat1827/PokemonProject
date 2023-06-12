using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hPBar;
    [SerializeField] GameObject expBar;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color slpColor;
    [SerializeField] Color parColor;
    [SerializeField] Color frzColor;
    Dictionary<ConditionID, Color> statusColors;

    Pokemon _pokemon;
    public void SetData(Pokemon pokemon)
    {
        _pokemon = pokemon;
        nameText.text = pokemon.Base.Name;
        levelText.text = "Lv" + pokemon.Level;
        hPBar.SetHP((float)pokemon.HP / pokemon.MaxHP);
        SetExp();

        statusColors = new Dictionary<ConditionID, Color>
        {
            { ConditionID.psn,psnColor},
            { ConditionID.brn,brnColor },
            { ConditionID.slp,slpColor },
            { ConditionID.par,parColor },
            { ConditionID.frz,frzColor },
        };
        SetStatusText();
        _pokemon.OnStatusChanged += SetStatusText;
    }

    void SetStatusText()
    {
        if(_pokemon.Status == null)
        {
            statusText.text = "";
        }
        else
        {
            statusText.text = _pokemon.Status.ID.ToString().ToUpper();
            statusText.color = statusColors[_pokemon.Status.ID];
        }
    }

    public IEnumerator UpdateHp()
    {
        if (_pokemon.HpChanged)
        {
            yield return hPBar.SetHPSmooth((float)_pokemon.HP / _pokemon.MaxHP);
            _pokemon.HpChanged = false;
        }
      
    }

    public void SetExp()
    {
        if (expBar == null) return;

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1,1);

    }

    public IEnumerator SetExpSmooth()
    {
        if (expBar == null) yield break;

        float normalizedExp = GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp,1.5f).WaitForCompletion();

    }



    float  GetNormalizedExp()
    {
        int currLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level);
        int nextLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level + 1);

        float normalizedExp = (float)(_pokemon.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }
}
