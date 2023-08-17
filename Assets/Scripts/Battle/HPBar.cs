using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBar : MonoBehaviour
{

    [SerializeField] GameObject health;
    // Start is called before the first frame update

    public bool IsUpdating { get; private set; }

    void Start()
    {
        health.transform.localScale = new Vector3(1f, 1f);
    }

    public void SetHP(float hpNormalized)
    {
        health.transform.localScale = new Vector3(hpNormalized, 1f);
    }

    public IEnumerator SetHPSmooth(float newHP)
    {
        IsUpdating = true;

        float curHP = health.transform.localScale.x;
        float changeAmt = curHP - newHP;
        while(curHP - newHP > Mathf.Epsilon)
        {
            curHP -= changeAmt * Time.deltaTime;
            health.transform.localScale = new Vector3(curHP, 1f);
            yield return null;
        }
        health.transform.localScale = new Vector3(newHP, 1f);

        IsUpdating = false;
    }
}
