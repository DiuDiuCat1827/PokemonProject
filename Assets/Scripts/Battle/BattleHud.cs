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
        if(_pokemon != null)
        {
            _pokemon.OnHPChanged -= UpdateHP;
            _pokemon.OnStatusChanged -= SetStatusText;
        }

        _pokemon = pokemon;
        nameText.text = pokemon.Base.Name;
        SetLevel();
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
        _pokemon.OnHPChanged += UpdateHP;
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

    public void UpdateHP()
    {
        StartCoroutine(UpdateHPAsync());
      
    }

    public IEnumerator UpdateHPAsync()
    {
        yield return hPBar.SetHPSmooth((float)_pokemon.HP / _pokemon.MaxHP);
    }

    public void SetExp()
    {
        if (expBar == null) return;

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1,1);

    }

    public IEnumerator SetExpSmooth(bool reset=false)
    {
        if (expBar == null) yield break;

        if (reset)
        {
            expBar.transform.localScale = new Vector3(0, 1, 1);
        }

        float normalizedExp = GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp,1.5f).WaitForCompletion();

    }

    public void SetLevel()
    {
        levelText.text = "Lv" + _pokemon.Level;
    }

    float  GetNormalizedExp()
    {
        int currLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level);
        int nextLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level + 1);

        float normalizedExp = (float)(_pokemon.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }

    public void ClearData()
    {
        if(_pokemon != null)
        {
            _pokemon.OnHPChanged -= UpdateHP;
            _pokemon.OnStatusChanged -= SetStatusText;
        }
    } 

    public IEnumerator WaitForHPUpdate()
    {
        yield return new WaitUntil( () => hPBar.IsUpdating == false);
    }
}
