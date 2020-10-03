﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    [Header("Pause Panels")]
    [SerializeField] GameObject pauseHUD;
    [SerializeField] GameObject[] panels;
    bool isPaused = false;

    [Header("Pause Texts")]
    [SerializeField] Image[] pauseTexts;
    [SerializeField] Sprite[] normalTexts;
    [SerializeField] Sprite[] coloredTexts;
    int currentlySelected = 0;
    int previouslySelected = -1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0;

        pauseHUD.SetActive(isPaused);
        SwitchToPausePanel(0);
    }

    // switch to selected pause panel and highlight respective pause text
    public void SwitchToPausePanel(int index)
    {
        if (index != currentlySelected)
        {
            previouslySelected = currentlySelected;
            currentlySelected = index;

            pauseTexts[previouslySelected].sprite = normalTexts[previouslySelected];
            pauseTexts[previouslySelected].SetNativeSize();

            pauseTexts[currentlySelected].sprite = coloredTexts[currentlySelected];
            pauseTexts[currentlySelected].SetNativeSize();

            panels[currentlySelected].SetActive(true);
            panels[previouslySelected].SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1;

        pauseHUD.SetActive(isPaused);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1;

        SceneManager.LoadScene(0);
    }
}