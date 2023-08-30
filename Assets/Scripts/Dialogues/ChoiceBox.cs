using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ChoiceBox : MonoBehaviour
{
    [SerializeField] ChoiceText choiceTextPrefab;

    bool ChoiceSelected = false;

    List<ChoiceText> choiceTexts;
    int currentChoice;

    public IEnumerator ShowChoices(List<string> choices, Action<int> onChoicesSelected)
    {
        ChoiceSelected = false;
        currentChoice = 0;

        gameObject.SetActive(true);

        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        choiceTexts = new List<ChoiceText>();

        foreach(var choice in choices)
        {
            var choiceTextObj = Instantiate(choiceTextPrefab, transform);
            choiceTextObj.TextField.text = choice;
            choiceTexts.Add(choiceTextObj);
        }

        yield return new WaitUntil(() => ChoiceSelected == true);

        onChoicesSelected?.Invoke(currentChoice);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            currentChoice++;
        }else if (Input.GetKeyDown(KeyCode.W))
        {
            currentChoice--;
        }

        currentChoice = Mathf.Clamp(currentChoice, 0, choiceTexts.Count - 1);

        for(int i = 0; i < choiceTexts.Count; i++)
        {
            choiceTexts[i].SetSelected(i == currentChoice);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            ChoiceSelected = true;
        }
    }
}
