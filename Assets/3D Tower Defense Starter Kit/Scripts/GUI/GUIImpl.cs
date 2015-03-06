/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;


//implementation of the GUILogic.cs script with customized behavior
public class GUIImpl : MonoBehaviour
{
    //reference to the main logic
    private GUILogic gui;

    //PARTICLE FX
    public GameObject buildFx;

    //GUI ELEMENTS
    public pan panels = new pan();
    public btn buttons = new btn();
    public lbl labels = new lbl();
    public snd sound = new snd();
    public con control = new con();

    //time value to insert a delay between mouse clicks
    //(we do not want to show the upgrade menu instantly after a tower was bought)
    private float time;

    //camera starting rotation, only needed on mobiles to reset the camera after leaving a tower
    private Vector3 initRot;


    //get reference of the main GUI script
    void Awake()
    {
        gui = GetComponent<GUILogic>();
    }


    void Start()
    {
        //set camera starting rotation (mobile or not)
        initRot = gui.raycastCam.transform.eulerAngles;

        //instantiate self control mouse position indicator at a non visible position
        //so SelfControl.cs has access to it and can change its position when we right click on a tower to control it
        control.crosshair = (GameObject)Instantiate(control.crosshair, new Vector3(0, -200, 0), Quaternion.identity);
        //the same with AimPrefab, it indicates the flight path of projectiles of a tower
        control.aimIndicator = (GameObject)Instantiate(control.aimIndicator, new Vector3(0, -200, 0), Quaternion.identity);


        //NGUI BUTTON DELEGATES
        //hook up method ExitMenu() to launch on clicking buttons of the exit menu
        foreach (Transform button in panels.main.transform)
        {
            UIEventListener.Get(button.gameObject).onClick += ExitMenu;
        }

        //hook up method ShowButtons() to launch on clicking the main tower button
        UIEventListener.Get(buttons.mainButton).onClick += ShowButtons;

        //hook up method CreateTower() to launch on clicking a tower button
        foreach (Transform button in buttons.towerButtons.transform)
        {
            UIEventListener.Get(button.gameObject).onClick += CreateTower;
        }

        //hook up method SellTower() to launch on clicking the sell tower button of the upgrade menu
        UIEventListener.Get(buttons.button_sell).onClick += SellTower;

        //hook up method UpgradeTower() to launch on clicking the upgrade tower button of the upgrade menu
        UIEventListener.Get(buttons.button_upgrade).onClick += Upgrade;

        //hook up method DisableMenu() to launch on clicking the button to disable tooltips and upgrade menus
        UIEventListener.Get(buttons.button_abort).onClick += DisableMenus;

        //hook up method DisableSelfControl() to launch on clicking the button for leaving a tower and disable it
        UIEventListener.Get(buttons.button_exit).onClick += DisableSelfControl;
        NGUITools.SetActive(buttons.button_exit, false);

        //if we play the mobile version, hook up method InteractiveShoot() to launch on clicking the shoot button
        if (gui.mobile)
            UIEventListener.Get(buttons.mobile_shoot).onClick += InteractiveShoot;
    }


    //show all tower buttons
    public void ShowButtons(GameObject button)
    {
        //if the exit menu is active, do nothing
        if (panels.main.activeInHierarchy)
            return;

        //fade out or fade in the tower buttons depending on their visibility
        gui.StartCoroutine("FadeOut", buttons.towerButtons);
        gui.StartCoroutine("FadeIn", buttons.towerButtons);
        //disable tooltip and upgrade panel
        DisableMenus(null);
    }


    //fade out tooltip and upgrade panel and deactivate all current selections
    public void DisableMenus(GameObject button)
    {
        //toggle upgrade menu visibility value
        SV.showUpgrade = false;

        //fade out tooltip and upgrade panel
        gui.StartCoroutine("FadeOut", panels.tooltip);
        gui.StartCoroutine("FadeOut", panels.upgradeMenu);
        //destroy current selections and free variables
        gui.CancelSelection(true);

        //disable range indicator visibility if set
        if (gui.towerBase) gui.towerBase.rangeInd.renderer.enabled = false;
    }


    //update is called every frame,
    //here we check if the user pressed the escape button (desktop or mobile)
    void Update()
    {
        CheckESC();

        //don't check against grids or towers if our mouse is over the gui
        //or the main menu is shown
        if (UICamera.hoveredObject || SV.showExit)
            return;
        else if (Input.GetMouseButtonUp(0) && !SV.selection 
                && !SV.control && !gui.currentTower &&!gui.mobile)
        {
            //disable all menus if we have no floating tower, aren't controlling
            //any tower and clicked somewhere in the game when not over gui elements
            //doesn't work on mobiles, since UICamera.hoveredObject is always empty
            DisableMenus(null);
        }

        //react to GUILogic.cs raycasts
        ProcessGrid();
        ProcessTower();
    }


    //checks if the user presses the esc button and
    //displays the exit menu
    void CheckESC()
    {
        //on ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //if we control a tower
            if (SV.control)
            {
                //remove control mechanism
                DisableSelfControl(buttons.button_exit);
            }

            //if exit menu isn't already active display it
            gui.StartCoroutine("FadeIn", panels.main);
            //fade out all tower buttons
            gui.StartCoroutine("FadeOut", buttons.towerButtons);
            //disable active selections
            DisableMenus(null);
            //toggle exit menu visibility variable
            SV.showExit = true;
            //pause the game
            Time.timeScale = 0.0001f;
        }
    }


    void ProcessGrid()
    {
        //we don't have a floating tower ready for purchase, return
        if (!SV.selection)
            return;
        else if (!gui.CheckIfGridIsFree())
        {
            //grid is contained in our occupied grid list and therefore not available
            //we can't place our tower on this grid, move our tower out of sight
            //so it does not get rendered by the camera anymore
            SV.selection.transform.position = new Vector3(0, -200, 0);
        }
        else
        {
            //the targeted grid is available
            //place tower on top of this grid
            SV.selection.transform.position = gui.currentGrid.transform.position;
			
			//rotate turret to match the grid rotation
            if(gui.towerBase.turret)
                gui.towerBase.turret.rotation = gui.currentGrid.transform.rotation;

            //we bought a tower by pressing the left mouse button while over the grid
            if (Input.GetMouseButtonUp(0))
            {
                //--> purchase successful
                BuyTower();
            }
        }
    }


    void ProcessTower()
    {
        //get current tower the mouse is over
        GameObject tower = gui.currentTower;

        if (tower == null) return;

        //if we select a tower for purchase, and left click - while over a free grid - to place/buy it,
        //this would open the upgrade menu instantly (because the click on this tower is also recognized as upgrade click)
        //so we check whether between those (two) clicks some time has passed (a half second) and only then open the the upgrade menu. 
        //also we do not want to open the upgrade menu if we control a tower
        if (Input.GetMouseButtonUp(0) && time + 0.5f < Time.time && !SV.control)
        {
            //finally, show upgrade menu of this tower, but stop active refreshing before that
            ShowUpgradeMenu(tower);
        }

        //get time stamp of right mouse button, used below
        if (Input.GetMouseButtonDown(1))
        {
            time = Time.time;
        }

        //check whether we released the right mouse button soon enough to simulate a simple right click
        //(release less than a half second) and enable self control mode
        //on mobile devices we don't use the right mouse button, instead we use a simple tap on a tower
        //when the upgrade menu of this tower is already shown - this simulates that we need a double tap
        if ((Input.GetMouseButtonUp(1) && time + 0.5f > Time.time)
            || gui.mobile && Input.GetMouseButtonDown(0) && SV.showUpgrade && gui.upgrade.gameObject == tower)
        {
            //attach self control script to this tower
            EnableSelfControl();
        }
    }


    //instantiate floating tower when a tower button was clicked
    void CreateTower(GameObject button)
    {
        //the upgrade menu is shown, disable this menu and the range indicator
        if (SV.showUpgrade && gui.towerBase)
        {
            gui.towerBase.rangeInd.renderer.enabled = false;
            SV.showUpgrade = false;
        }

        //parse the name string of the tower button to an integer,
        //indicating our TowerManager tower index
        //(first button = 0 = first tower, second button = second tower in the list etc.)
        int index;

        if (int.TryParse(button.name, out index))
        {
            //try to instantiate floating tower
            gui.InstantiateTower(index);

            //show tool tip menu
            ShowTooltipMenu(index);
        }
        else
        {
            //Debug.Log("Tower button '" + button.name + "' couldn't be converted to int.");
        }

        //if a tower was created, toggle range indicator visibility on
        if(SV.selection)
            gui.towerBase.rangeInd.renderer.enabled = true;
    }


    //buy floating tower and place it on a grid
    void BuyTower()
    {
        //play tower bought sound
        AudioManager.Play2D(sound.build);

        //let PoolManager spawn a build fx prefab at the placed tower
        if(buildFx)
            PoolManager.Pools["Particles"].Spawn(buildFx, SV.selection.transform.position, Quaternion.identity);

        //buy selected tower
        gui.BuyTower();

        //free active selections
        gui.CancelSelection(false);

        //disable range indicator visibility if set
        if (gui.towerBase) gui.towerBase.rangeInd.renderer.enabled = false;

        //fade out tooltip panel
        gui.StartCoroutine("FadeOut", panels.tooltip);

        //set current time to prevent a simulated double click, see above in ProcessTower()
        time = Time.time;
    }


    void SellTower(GameObject button)
    {
        //we sold this tower
        //play tower sold sound
        AudioManager.Play2D(sound.sell);

        //get selected tower, for that the upgrade menu is active
        GameObject tower = gui.upgrade.transform.parent.gameObject;
        
        //sell the selected tower, add resources
        gui.SellTower(tower);
        //highlight this tower as active selection
        SV.selection = tower;
        //frees the selection by destroying the selected tower
        DisableMenus(null);
    }


    //show upgrade menu for the passed tower
    public void ShowUpgradeMenu(GameObject tower)
    {
        //enable tooltip and upgrade panel
        gui.StartCoroutine("FadeIn", panels.tooltip);
        gui.StartCoroutine("FadeIn", panels.upgradeMenu);
        //toggle upgrade menu visibility variable
        SV.showUpgrade = true;

        //disable previous range indicator if one is set already
        //(old one from another tower)
        if (gui.towerBase)
        {
            gui.towerBase.rangeInd.renderer.enabled = false;
        }

        //set tower properties
        gui.SetTowerComponents(tower);

        //store current upgrade level
        int curLvl = gui.upgrade.curLvl;
        //store necessary upgrade option info for later use
        UpgOptions upgOptions = gui.upgrade.options[curLvl];

        //show tower range indicator
        gui.towerBase.rangeInd.renderer.enabled = true;

        //set UI label properties with information of this tower
        labels.towerName.text = gui.upgrade.gameObject.name;
        labels.properties.text = "Current lvl: " + curLvl + "\n" +
                                "Radius: " + upgOptions.radius + "\n" +
                                "Damage: " + upgOptions.damage + "\n" +
                                "Shoot Delay: " + upgOptions.shootDelay + "\n" +
                                "Target Count: " + upgOptions.targetCount;

        //initialize upgrade and sell price array value for multiple resources
        float[] sellPrice = gui.GetSellPrice();
        float[] upgradePrice = gui.GetUpgradePrice();
        //only display the upgrade button, if there IS a level for upgrading left
        //and we have enough money for upgrading to the next level, check every resource
        //initialize boolean as true
        bool affordable = true;

        for (int i = 0; i < upgradePrice.Length; i++)
        {
            //set sell price resource label to the actual value
            labels.sellPrice[i].text = "$" + sellPrice[i];

            //check if we can buy another upgrade level
            //if not, erase price values
            if(!gui.AvailableUpgrade())
            {
                affordable = false;
                labels.price[i].text = "";
                continue;
            }

            //set price label for upgrading to the next level
            labels.price[i].text = "$" + upgradePrice[i];
        }

        //there is a level to upgrade left, so check if we can afford this tower upgrade
        if(affordable)
            affordable = gui.AffordableUpgrade();

        //the upgrade is still affordable
        if (affordable)
            //in case the upgrade button was deactivated we activate it here again
            NGUITools.SetActive(buttons.button_upgrade, true);
        else
            //we can't afford an upgrade, disable upgrade button
            NGUITools.SetActive(buttons.button_upgrade, false);
    }


    //upgrade tower
    void Upgrade(GameObject button)
    {
        //tower upgrade successful
        //play tower upgrade sound
        AudioManager.Play2D(sound.upgrade);

        //execute upgrade
        gui.UpgradeTower();

        //refresh upgrade panel with new values
        ShowUpgradeMenu(gui.upgrade.gameObject);
    }


    //enable tooltip menu and disable upgrade menu if active
    public void ShowTooltipMenu(int index)
    {
        //fade in tooltip panel, fade out upgrade panel
        gui.StartCoroutine("FadeIn", panels.tooltip);
        gui.StartCoroutine("FadeOut", panels.upgradeMenu);
        //toggle upgrade menu visibility value
        SV.showUpgrade = false;

        //store tower base properties from TowerManager lists
        //(in case we haven't instantiated a tower because it
        //wasn't affordable, we can't access an instance and
        //have to use these components pre-stored in TowerManager.cs)
        TowerBase baseOptions = gui.towerScript.towerBase[index];
        //store necessary upgrade option info for later use
        UpgOptions upgOptions = gui.towerScript.towerUpgrade[index].options[0];

        //set all information related to tower properties,
        //such as tower name, properties and initial price
        labels.towerName.text = gui.towerScript.towerNames[index];
        labels.properties.text = "Projectile: " + baseOptions.projectile.name + "\n" +
                                "Radius: " + upgOptions.radius + "\n" +
                                "Damage: " + upgOptions.damage + "\n" +
                                "Shoot Delay: " + upgOptions.shootDelay + "\n" +
                                "Targets: " + baseOptions.myTargets;

        //set visible label price text for each resource
        for (int i = 0; i < GameHandler.resources.Length; i++)
            labels.price[i].text = "$" + upgOptions.cost[i];
    }


    //game exit menu
    public void ExitMenu(GameObject button)
    {
        //toggle exit menu visibility
        SV.showExit = false;
        //resume game
        Time.timeScale = 1;

        //no matter what we clicked (main menu or cancel),
        //we hide the exit menu again
        gui.StartCoroutine("FadeOut", panels.main);

        //handle exit button
        if (button.name == "MainMenu")
        {
            //load our first scene - the main menu
            Application.LoadLevel(0);
        }
    }


    //attach self control component to the selected tower
    public void EnableSelfControl()
    {
        //we right-clicked/double tapped the tower we control already,
        //or the main menu is shown, do nothing (return)
        if (SV.control || SV.showExit)
            return;

        //disable the main tower button and fade out all tower buttons
        //while controlling a tower we don't want them to be visible
        NGUITools.SetActive(buttons.mainButton, false);
        gui.StartCoroutine("FadeOut", buttons.towerButtons);

        //ensure to disable upgrade menu and old range indicator if they're enabled
        if (SV.showUpgrade)
        {
            //fade out tooltip and upgrade menu
            gui.StartCoroutine("FadeOut", panels.tooltip);
            gui.StartCoroutine("FadeOut", panels.upgradeMenu);
            //toggle upgrade menu visibility
            SV.showUpgrade = false;
            //hide tower range indicator
            gui.towerBase.rangeInd.renderer.enabled = false;
        }

        //add control component to the desired tower and cache it
        SV.control = gui.currentTower.transform.gameObject.AddComponent<SelfControl>();
        //initialize self control variables
        SV.control.Initialize(gameObject, control.crosshair, control.aimIndicator,
                              control.towerHeight, gui.mobile);
        //update components to controlled tower
        gui.SetTowerComponents(gui.currentTower);
        //check remaining reload time for this tower
        StartCoroutine("DrawReload");

        //enable mobile shot button (in mobile mode)
        if (gui.mobile)
            NGUITools.SetActive(buttons.mobile_shoot, true);

        //enable exit tower button
        NGUITools.SetActive(buttons.button_exit, true);
    }


    //only mobile, shot button method for self control script
    public void InteractiveShoot(GameObject button)
    {
        SV.control.Attack();
    }


    //terminate interactive tower control
    public void DisableSelfControl(GameObject button)
    {
        //remove control mechanism    
        SV.control.Terminate();
        //stop manipulating the reloading slider value
        StopCoroutine("DrawReload");

        //set main tower button and all tower buttons back to active
        NGUITools.SetActive(button, false);
        NGUITools.SetActive(buttons.mainButton, true);

        //on mobile devices, hide the mobile shot button again
        //and reset the camera to the initial rotation using iTween
        if (gui.mobile)
        {
            NGUITools.SetActive(buttons.mobile_shoot, false);
            iTween.RotateTo(gui.raycastCam.gameObject, iTween.Hash("rotation", initRot, "time", 1f));
        }
    }


    //animate frames of reload texture. Gets started by SelfControl.cs in Attack()
    //if the controlled tower attacked
    public IEnumerator DrawReload()
    {
        //divide delay / total frames so we get waiting time after each frame
        //in order to complete it within the given delay time
        //cache delay for shooting at this level
        float shootDelay = gui.upgrade.options[gui.upgrade.curLvl].shootDelay;

        //calculate time when this tower can shoot again
        float remainTime = gui.towerBase.lastShot + shootDelay - Time.time;

        //cache slider, get and set current reload time in % to the slider
        UISlider slider = control.relSlider;
        float curValue = 1 - (remainTime / shootDelay);
        slider.sliderValue = curValue;

        //remainTime > 0 means this tower shot already within the last few seconds,
        //so we change the color indicating a reload
        if (remainTime > 0)
            control.relSprite.color = Color.yellow;
        else
        {
            //there is no need to animate the slider,
            //set color to finished one and break out here
            control.relSprite.color = Color.green;
            yield break;
        }

        //lerping time value
        float t = 0f;
        //as long as the bar isn't full
        while(t < 1)
        {
            //increase time value depending on remaining reload time
            t += Time.deltaTime/remainTime;
            //set slider value by lerping from current value to 1 = full
            slider.sliderValue = Mathf.Lerp(curValue, 1f, t);
            yield return null;
        }

        //the bar is full - we can shoot again, change bar color to finished
        control.relSprite.color = Color.green;
    }


    //NGUI PANEL, BUTTON, LABEL elements
    //SOUND effects and SELF CONTROL variables

    [System.Serializable]
    public class pan
    {
        public GameObject main; //exit menu panel
        public GameObject upgradeMenu; //upgrade menu panel
        public GameObject tooltip;  //tooltip menu panel
    }


    [System.Serializable]
    public class btn
    {
        public GameObject mainButton;   //button for activating other tower buttons
        public GameObject towerButtons; //the parent of all tower buttons

        public GameObject button_sell;  //button to sell the selected tower
        public GameObject button_upgrade;   //button to upgrade the selected tower
        public GameObject button_abort; //button to disable the upgrade menu

        public GameObject button_exit;  //button to leave self-control mode

        public GameObject mobile_shoot; //button to shoot on mobile devices
    }


    [System.Serializable]
    public class lbl
    {
        public UILabel towerName;   //label for the tower name
        public UILabel properties;  //lable for the tower properties (tooltip/upgrade menu)
        public UILabel[] price;   //initial price of the tower
        public UILabel[] sellPrice;   //value at which the tower will be sold
    }


    [System.Serializable]
    public class snd
    {
        public AudioClip build; //sound to play on placing a bought tower
        public AudioClip sell;  //sound to play on selling a selected tower
        public AudioClip upgrade;   //sound to play on upgrading a selected tower
    }


    //SELF CONTROL VARIABLES
    [System.Serializable]
    public class con
    {
        public GameObject crosshair; //mouse position crosshair prefab while controlling a tower
        //prefab showing an animated line between the self controlled tower and our mouse position
        public GameObject aimIndicator;
        //extra height for our camera, which will be added on selfcontrol
        //- to "sit" on top of a tower while controlling it
        public float towerHeight = 10f;
        public UISlider relSlider;  //reloading progress bar
        public UIFilledSprite relSprite;    //foreground bar sprite for changing its color
    }
}