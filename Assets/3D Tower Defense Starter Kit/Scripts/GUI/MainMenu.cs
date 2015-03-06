/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//MainMenu.cs displays the GUI of our Title Menu scene "Menu"
public class MainMenu : MonoBehaviour
{
    //start button to start the actual game
    public GameObject startButton;
    //main level name to load
    public int sceneIndex;
    //loading progress on webplayer builds,
    //displays how long it takes to stream the game scene
    public UILabel progressText;

    //SOUND EFFECTS
    public AudioClip introMusic;


    void Start()
    {
        if (Application.levelCount == 1)
        {
            //point to readme if build settings are missing
            Debug.LogError("Important: Please read the README file before playing the game!");
        }

        //NGUI - hook up method LoadButton() on startButton click
        UIEventListener.Get(startButton).onClick += LoadButton;

        //play main menu intro sound
        AudioManager.Play(introMusic);
    }


    void Update()
    {
        //check for ESC for closing this application
        //(back hardware key on mobile devices)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }


    //stream game scene
    void LoadButton(GameObject button)
    {
        //only invoke streaming once
        if(!IsInvoking("LoadGame"))
        {
            //start invoking LoadGame() every 0.2 seconds,
            //this sets our scene load progress value
            InvokeRepeating("LoadGame", 0f, 0.2f);

            //disable start button and enable streaming progress
            startButton.SetActive(false);
            progressText.gameObject.SetActive(true);
        }
    }


    //calculate and set loading progress of the game level
    void LoadGame()
    {
        //the game scene is ready
        if (Application.CanStreamedLevelBeLoaded(sceneIndex))
        {
            //set progress to 100, so that gets displayed while switching scenes 
            progressText.text = 100 + "%";
            //load the game scene
            Application.LoadLevel(sceneIndex);
        }
        else
        {
            //we're still loading the game scene, get the current progress and store it
            progressText.text = ((int)(Application.GetStreamProgressForLevel(sceneIndex) * 100)) + "%";
        }
    }
}