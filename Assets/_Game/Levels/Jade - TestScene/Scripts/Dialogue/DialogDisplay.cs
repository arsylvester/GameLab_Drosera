﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogDisplay : MonoBehaviour
{
    [SerializeField] public GameObject nextSceneButton;
    [SerializeField] public GameObject continueButton;

    [SerializeField] public Conversation conversation;
    [SerializeField] public GameObject speakerLeft;
    [SerializeField] public GameObject speakerRight;

    private SpeakerUI speakerUILeft;
    private SpeakerUI speakerUIRight;

    private int activateLineIndex = 0;

    private void Start()
    {
        speakerUILeft = speakerLeft.GetComponent<SpeakerUI>();
        speakerUIRight = speakerRight.GetComponent<SpeakerUI>();

        speakerUILeft.Speaker = conversation.speakerLeft;
        speakerUIRight.Speaker = conversation.speakerRight;

        AdvanceConversation();
        continueButton.SetActive(true);
    }

    public void NextSentences()
    {
        AdvanceConversation();
    }

    public void AdvanceConversation()
    {
        if(activateLineIndex < conversation.lines.Length)
        {
            DisplayLine();
            activateLineIndex += 1;
        }
        else
        {
            speakerUILeft.Hide();
            speakerUIRight.Hide();
            activateLineIndex = 0;
            // set scene change 
            nextSceneButton.SetActive(true);
            continueButton.SetActive(false);
        }
    }

    public void DisplayLine()
    {
        Line line = conversation.lines[activateLineIndex];

        Character character = line.character;

        if(speakerUILeft.SpeakerIs(character))
        {
            SetDialog(speakerUILeft, speakerUIRight, line.text);
        }
        else
        {
            SetDialog(speakerUIRight, speakerUILeft, line.text);
        }
    }

    public void SetDialog(SpeakerUI activeSpeakerUI, SpeakerUI inactiveSpeakerUI, string text)
    {
        activeSpeakerUI.Show();
        inactiveSpeakerUI.Hide();

        StopAllCoroutines();
        StartCoroutine(EffectTypewriter(text, activeSpeakerUI));
    }

    IEnumerator EffectTypewriter(string text, SpeakerUI activeSpeakerUI)
    {
        activeSpeakerUI.Dialog = "";
        foreach (char character in text.ToCharArray())
        {
            activeSpeakerUI.Dialog += character;
            yield return null;
        }
    }
}