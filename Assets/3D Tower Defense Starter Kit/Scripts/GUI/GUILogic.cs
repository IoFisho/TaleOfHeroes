/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//GUILogic.cs is the base for all GUI actions, such as tower buttons and tower purchase with grid selection,
//you can implement these methods in your own GUI script (e.g. see GUIImpl.cs)
public class GUILogic : MonoBehaviour
{
    //SCRIPT REFERENCES
    public TowerManager towerScript;    //access to tower prefab list
    public GridManager gridScript;  //access to grid list so we can check whether a grid is free or occupied
    public Camera raycastCam; //camera which casts a ray against grids and towers (usually the main camera)

    //RAYS & RAYCASTHITS
    private Ray ray;    //ray for grid and tower detection
    private RaycastHit gridHit;     //raycast has hit a grid
    private RaycastHit towHit;      //raycast has hit a tower

    //TOWER PROPERTIES
    private Transform towerContainer;   //gameobject that should hold all instantiated towers
    [HideInInspector]
    public TowerBase towerBase;    //tower base properties of floating tower
    [HideInInspector]
    public Upgrade upgrade;    //tower upgrade properties of floating tower
    [HideInInspector]
    public GameObject currentGrid;     //current grid the mouse is over
    [HideInInspector]
    public GameObject currentTower;     //current tower the mouse is over

    //indicating which button we clicked represented in the TowerManager tower index
    int index = 0;

    //GUI PROPERTIES
    public GameObject[] invisibleWidgets;
    //dictionary with invisible UI elements at start, initialized by array 'invisibleWidgets' in Start()
    public Dictionary<GameObject, List<UIWidget>> invisibleDic = new Dictionary<GameObject, List<UIWidget>>();

    //warning message text (empty at start)
    public UILabel errorText;

    //does this application run on mobile devices?
    //this variable determines the behaviour of touch inputs in GUIImpl.cs
    public bool mobile;


    //null reference checks and initialization of invisible widgets
    void Start()
    {
        //print errors if scripts are not set
        if (towerScript == null)
            Debug.LogWarning("GUI TowerManager not set");

        if (gridScript == null)
            Debug.LogWarning("GUI GridManager not set");

        //if this application runs on android or iphone set mobile boolean to true
        if (Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer)
            mobile = true;

        //get tower container gameobject
        towerContainer = GameObject.Find("Tower Manager").transform;

        //store invisible widgets so they can be activated later
        foreach (GameObject gui_Obj in invisibleWidgets)
        {
            StoreInvisibleWidgets(gui_Obj);
            //disable widget
            NGUITools.SetActive(gui_Obj, false);
        }
    }


    //make listed widgets invisible and disable them by setting their alpha color to zero
    public void StoreInvisibleWidgets(GameObject gObj)
    {
        //add widget to dictionary for later access and reactivation
        invisibleDic.Add(gObj, new List<UIWidget>());
        //access all UI widget components of this UI gameobject
        UIWidget[] allWidgets = gObj.GetComponentsInChildren<UIWidget>();

        //loop through widgets and make them invisible
        for (int i = 0; i < allWidgets.Length; i++)
        {
            Color inviColor = allWidgets[i].color;
            inviColor.a = 0f;
            allWidgets[i].color = inviColor;

            //finally store the widget in the corresponding dictionary slot
            invisibleDic[gObj].Add(allWidgets[i]);
        }
    }


    //update is called every frame,
    //here we execute raycasts and input methods
    void Update()
    {
        //cast ray at our mouse position which then delivers input
        //for our RaycastHit 'towHit' and 'gridHit'
        ray = raycastCam.ScreenPointToRay(Input.mousePosition);

        //we do have a floating tower
        if (SV.selection)
        {
            //check if the ray hit a grid
            RaycastGrid();
        }
        //no floating tower is active
        else
        {
            //check if the ray hit a tower
            RaycastTower();
        }
    }


    //grid detection raycast
    void RaycastGrid()
    {
        //if ray has hit a grid (we cast against gridMask with very large distance)
        if (Physics.Raycast(ray, out gridHit, 300, SV.gridMask))
        {
            //show a visible line indicating the ray within the editor
            Debug.DrawLine(ray.origin, gridHit.point, Color.yellow);

            currentGrid = gridHit.transform.gameObject;
        }
        else
            currentGrid = null;
    }


    //tower detection raycast
    void RaycastTower()
    {
        //if ray has hit a tower (we cast against towerMask with very large distance)
        if (Physics.Raycast(ray, out towHit, 300, SV.towerMask))
        {
            //show a visible line indicating the ray within the editor
            Debug.DrawLine(ray.origin, towHit.point, Color.red);

            currentTower = towHit.transform.gameObject;
        }
        else
            currentTower = null;
    }


    //deactivate all current selections
    //destroy floating tower if necessary, and free used variables
    public void CancelSelection(bool destroySelection)
    {
        //destroy floating tower on parameter destroySelection == true
        if (destroySelection && SV.selection)
            Destroy(SV.selection);

        currentGrid = null;     //free current grid
        currentTower = null;    //free current tower
        SV.selection = null;    //free selection (just to make sure we don't have an empty reference)
        gridScript.ToggleVisibility(false);     //disable grid visibility
    }


    //short way to check if a tower is already placed on the current grid
    public bool CheckIfGridIsFree()
    {
        if (currentGrid == null || gridScript.GridList.Contains(currentGrid.name))
            return false;
        else
            return true;
    }


    //set components of the tower passed in for later use and access
    public void SetTowerComponents(GameObject tower)
    {
        upgrade = tower.GetComponent<Upgrade>();
        towerBase = tower.GetComponent<TowerBase>();
    }


    //short way to check if there's an upgrade level left to upgrade to
    public bool AvailableUpgrade()
    {
        if (upgrade == null)
        {
            Debug.Log("Can't check for available upgrades, upgrade script isn't set.");
            return false;
        }

        //initialize boolean
        bool available = true;
        //cache current tower level
        int curLvl = upgrade.curLvl;
        //check against total upgrade levels
        if (curLvl >= upgrade.options.Count - 1)
            available = false;

        return available;
    }


    //method to check if the next upgrade level is affordable
    public bool AffordableUpgrade()
    {
        if (upgrade == null)
        {
            Debug.Log("Can't check for affordable upgrade, upgrade script isn't set.");
            return false;
        }

        //initialize boolean
        bool affordable = true;
        //cache current tower level
        int curLvl = upgrade.curLvl;
        //first check if there's an upgrade level left to upgrade to
        if (AvailableUpgrade())
        {
            //loop through resources
            for (int i = 0; i < GameHandler.resources.Length; i++)
            {
                //check if we can afford an upgrade to this tower
                if (GameHandler.resources[i] < upgrade.options[curLvl + 1].cost[i])
                {
                    affordable = false;
                    break;
                }
            }
        }
        else
            affordable = false;

        return affordable;
    }


    //return upgrade price for the next tower level upgrade
    public float[] GetUpgradePrice()
    {
        //initialize upgrade price array value for multiple resources
        float[] upgradePrice = new float[GameHandler.resources.Length];
        //cache current tower level
        int curLvl = upgrade.curLvl;
        //first check if there's an upgrade level left to upgrade to
        //else return an empty float array
        if (AvailableUpgrade())
        {
            //loop through resources and get direct cost
            for (int i = 0; i < upgradePrice.Length; i++)
            {
                upgradePrice[i] = upgrade.options[curLvl + 1].cost[i];
            }
        }

        return upgradePrice;
    }


    //return total price at which the selected tower gets sold
    public float[] GetSellPrice()
    {
        //initialize sell price array value for multiple resources
        float[] sellPrice = new float[GameHandler.resources.Length];
        //cache current tower level
        int curLvl = upgrade.curLvl;

        //loop through resources
        for (int i = 0; i < sellPrice.Length; i++)
        {
            //loop through each upgrade purchased for this tower
            //calculate new sell price based on all upgrades and sellLoss
            for (int j = 0; j < curLvl + 1; j++)
                sellPrice[i] += upgrade.options[j].cost[i] * (1f - (towerScript.sellLoss / 100f));
        }

        return sellPrice;
    }


    //create active selection / floating tower on pressing a tower button
    public void InstantiateTower(int clickedButton)
    {
        //store the TowerManager tower index passed in as parameter
        index = clickedButton;

        //we clicked one of these tower buttons
        //if we already have a floating tower, destroy it and free selections
        if (SV.selection)
        {
            currentGrid = null;
            currentTower = null;
            Destroy(SV.selection);
        }

        //check if there are free grids left
        //no free grid left (list count is equal to grid count)
        if (gridScript.GridList.Count == gridScript.transform.GetChildCount())
        {
            //print a warning message
            StartCoroutine("DisplayError", "No free grids left for placing a new tower!");
            Debug.Log("No free grids left for placing a new tower!");
            return;
        }

        //initialize price array with total count of resources
        float[] price = new float[GameHandler.resources.Length];
        //cache selected upgrade options for further processment 
        UpgOptions opt = towerScript.towerUpgrade[index].options[0];

        //loop through resources
        //get needed resources (buy price) of this tower from upgrade list
        for (int i = 0; i < price.Length; i++)
            price[i] = opt.cost[i];

        //check in case we have not enough resources left, abort purchase
        for (int i = 0; i < price.Length; i++)
        {
            if (GameHandler.resources[i] < price[i])
            {
                StartCoroutine("DisplayError", "Not enough resources for buying this tower!");
                Debug.Log("Not enough resources for buying this tower!");
                //destroy selection
                CancelSelection(true);
                return;
            }
        }

        //all checks went through, we are able to purchase this tower
        //instantiate selected tower from TowerManager prefab list and thus create a floating tower
        SV.selection = (GameObject)Instantiate(towerScript.towerPrefabs[index], new Vector3(0, -200, 0), Quaternion.identity);
        //get new base properties of this tower instance
        towerBase = SV.selection.GetComponentInChildren<TowerBase>();
        //change name of the gameobject holding this component to the defined one in TowerManager names list
        towerBase.gameObject.name = towerScript.towerNames[index];
        //parent tower to the container gameobject
        SV.selection.transform.parent = towerContainer;
        //get new upgrade properties of this tower instance
        upgrade = SV.selection.GetComponentInChildren<Upgrade>();
        //disable its base properties, so while moving/placing the tower around, it can not attack
        towerBase.enabled = false;
        //show all grid renderers to see where we could (or could not) place the tower
        gridScript.ToggleVisibility(true);
    }


    //enables a tower on a grid after buying it
    public void BuyTower()
    {
        //reduce resources by the tower price
        for (int i = 0; i < GameHandler.resources.Length; i++)
            GameHandler.resources[i] -= towerScript.towerUpgrade[index].options[0].cost[i];

        //add selected grid to the grid list so it is not available / free anymore
        gridScript.GridList.Add(currentGrid.name);
        //change this grid color to the full-material for showing it is now occupied
        currentGrid.transform.renderer.material = gridScript.gridFullMat;
        //activate tower script and disable tower range indicator
        towerBase.enabled = true;
        towerBase.rangeInd.renderer.enabled = false;

        //get TowerRotation script and activate if it has one
        if (towerBase.turret)
            towerBase.turret.gameObject.GetComponent<TowerRotation>().enabled = true;
    }


    //upgrades the selected tower to the next level
    public void UpgradeTower()
    {
        //increase tower level and adjust range indicator properties
        upgrade.LVLchange();
        
        //reduce our resources
        for (int i = 0; i < GameHandler.resources.Length; i++)
            GameHandler.resources[i] -= upgrade.options[upgrade.curLvl].cost[i];
    }


    //sells the selected tower
    public void SellTower(GameObject tower)
    {
        float[] sellPrice = GetSellPrice();

        //add sell price to our resources
        for(int i = 0; i < sellPrice.Length; i++)
            GameHandler.resources[i] += sellPrice[i];    

        //define ray with down direction to get the grid beneath
        Ray ray = new Ray(tower.transform.position + new Vector3(0, 0.5f, 0), -transform.up);
        RaycastHit hit;

        //free grid the tower is standing on
        //raycast downwards of the tower against our grid mask to get the grid
        if (Physics.Raycast(ray, out hit, 20, SV.gridMask))
        {
            Transform grid = hit.transform;
            //remove grid from the "occupied"-list
            gridScript.GridList.Remove(grid.name);
            //change grid material to indicate it is free again
            grid.renderer.material = gridScript.gridFreeMat;
        }
    }


	//volume value callback method, called by slider located under
    //UI Root (2D) > Camera > Anchor_Menu > Panel_Main > Slider
    //simply sets the volume of the AudioListener (Camera) in the scene
    void OnVolumeChange(float value)
    {
        AudioListener.volume = value;
    }
	
	
    //pass in any text to this method and it will draw a message on the screen
    //used to show hints and warnings for the player when all grids are occupied,
    //not enough money to buy tower and similiar actions
    public IEnumerator DisplayError(string text)
    {
        //errorText is equal to passed text, do not execute again and return instead
        if (text == errorText.text)
            yield break;

        //set errorText to the passed in message text
        errorText.text = text;
        //start fading in the text
        StartCoroutine("FadeIn", errorText.gameObject);

        //show this message for 2 seconds
        yield return new WaitForSeconds(2);

        //if the error text has changed when we first set it,
        //(another message occured during the first one) we return at this position
        if (text != errorText.text)
            yield break;

        //start fading out the text
        StartCoroutine("FadeOut", errorText.gameObject);
        //wait fade out delay
        yield return new WaitForSeconds(0.2f);

        //after 2 seconds and when the text hasn't changed since then,
        //(this prevents that the second text would only display for less than 2 seconds)
        //reset/empty this text so no message gets shown anymore
        errorText.text = "";
    }


    //fade in widgets passed in
    public IEnumerator FadeIn(GameObject gObj)
    {
        //fade in within 0.2 seconds
        float duration = 0.2f;

        //if widget is not active, activate it
        //if the widget is already active, we don't have to fade it in
        if (!gObj.activeInHierarchy)
            NGUITools.SetActive(gObj, true);
        else
            yield break;

        //create alpha value
        float alpha = 1f;

        //add widget to invisible dictionary if it wasn't there already
        if(!invisibleDic.ContainsKey(gObj))
        {
            Debug.LogWarning("Widget " + gObj.name + " not defined as invisible Widget. Adding to dictionary.");
            StoreInvisibleWidgets(gObj);
        }

        //loop through widgets and set their alpha value to 1 using a NGUI color tween
        foreach (UIWidget widget in invisibleDic[gObj])
        {
            Color colorTo = widget.color;
            colorTo.a = alpha;
            TweenColor.Begin(widget.gameObject, duration, colorTo);
        }
    }


    //fade out widgets passed in
    public IEnumerator FadeOut(GameObject gObj)
    {
        //fade out within 0.2 seconds
        float duration = 0.2f;

        //if gameobject is already inactive, do nothing
        if (!gObj.activeInHierarchy)
            yield break;

        //create alpha value
        float alpha = 0f;

        //add widget to invisible dictionary if it wasn't there already
        if(!invisibleDic.ContainsKey(gObj))
        {
            Debug.Log("Widget " + gObj.name + " not defined as invisible Widget. Adding to dictionary.");
            StoreInvisibleWidgets(gObj);
        }

        //loop through widgets and set their alpha value to 0 using a NGUI color tween
        foreach (UIWidget widget in invisibleDic[gObj])
        {
            Color colorTo = widget.color;
            colorTo.a = alpha;
            TweenColor.Begin(widget.gameObject, duration, colorTo);
        }

        //wait till fade out was complete
        yield return new WaitForSeconds(duration);

        //disable UI elements
        NGUITools.SetActive(gObj, false);
    }
}
