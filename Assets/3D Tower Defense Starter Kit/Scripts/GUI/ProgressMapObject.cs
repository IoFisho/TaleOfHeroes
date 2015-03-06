/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//progress map object element property class
public class ProgressMapObject : MonoBehaviour
{  
    public UISlider slider; //slider for manipulating the progress value
    public UISprite sprite; //sprite component that shows the texture icon
    //object alive sprite name - moving icon on the progress bar
    public string objAliveSprite;
    //object killed sprite name - we display this texture on enemy death
    public string objDeadSprite;


    //when spawned, change the current sprite to the 'alive' one
    void OnSpawn()
    {
        slider.sliderValue = 0f;
        sprite.spriteName = objAliveSprite;
    }


    //executed by ProgressMap.cs
    public void CalculateProgress(float currentProgress)
    {
        //set object's progress
        slider.sliderValue = currentProgress;
    }
}