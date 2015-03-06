/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;

//userfriendly EditorWindow to setup new enemy prefabs out of models
public class EnemySetup : EditorWindow
{
	//enemy model slot within the window and prefab after instantiation
    public GameObject enemyModel;
    //healthbar prefab attached to the prefab
    public GameObject healthbar;
    //projector (blob shadow) prefab
    public GameObject projector;
    //collider type attached to the prefab
    public enum ColliderType
    {
        boxCollider,
        sphereCollider,
        capsuleCollider,
        meshCollider
    }
    //default ColliderType value
    public ColliderType colliderType = ColliderType.boxCollider;
    //enemy tag, default is 'Ground'
    public string tag = "Ground";
    //enemy layer number, default layer value is 'Enemies' 
    public int layer = LayerMask.NameToLayer("Enemies");
    //attach TweenMove component to this prefab?
    public bool attachTMove = true;
    //attach Properties component to this prefab?
    public bool attachProperties = true;
    //attach Rigidbody component to this prefab?
    public bool attachRigidbody = true;

	//collider bounds of enemy model
    Bounds totalBounds = new Bounds();
    //renderer components for calculating model bounds
    private Renderer[] renderers;


    // Add menu named "Enemy Setup" to the Window menu
    [MenuItem("Window/TD Starter Kit/Enemy Setup")]
    static void Init()
    {
        //get existing open window or if none, make a new one:
        EditorWindow.GetWindow(typeof(EnemySetup));
    }

	//draw custom editor window GUI
	void OnGUI()
    {
    	//display label and object field for enemy model slot 
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Enemy Model:");
        enemyModel = (GameObject)EditorGUILayout.ObjectField(enemyModel, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

		//display label and objectfield for healthbar prefab slot
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Healthbar Prefab:");
        healthbar = (GameObject)EditorGUILayout.ObjectField(healthbar, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

        //display label and objectfield for projector prefab slot
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Projector Prefab:");
        projector = (GameObject)EditorGUILayout.ObjectField(projector, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

		//display label and enum list for collider type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Collider Type:");
        colliderType = (ColliderType)EditorGUILayout.EnumPopup(colliderType);
        EditorGUILayout.EndHorizontal();

		//display label and tag field for enemy tag
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Enemy Tag:");
        tag = EditorGUILayout.TagField(tag);
        EditorGUILayout.EndHorizontal();

		//display label and layer field for enemy layer
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Enemy Layer:");
        layer = EditorGUILayout.LayerField(layer);
        EditorGUILayout.EndHorizontal();

		//display label and checkbox for TweenMove component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach TweenMove:");
        attachTMove = EditorGUILayout.Toggle(attachTMove);
        EditorGUILayout.EndHorizontal();

		//display label and checkbox for Properties component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach Properties:");
        attachProperties = EditorGUILayout.Toggle(attachProperties);
        EditorGUILayout.EndHorizontal();

		//display label and checkbox for Rigidbody component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach Rigidbody:");
        attachRigidbody = EditorGUILayout.Toggle(attachRigidbody);
        EditorGUILayout.EndHorizontal();

		//display info box below all settings
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("By clicking on 'Apply!' all chosen components are added and a prefab will be created next to your enemy model.", MessageType.Info);
        EditorGUILayout.Space();

		//apply button
        if (GUILayout.Button("Apply!"))
        {
        	//cancel further execution if no enemy model is set
            if (enemyModel == null)
            {
                Debug.LogWarning("No enemy model chosen. Aborting Enemy Setup execution.");
                return;
            }

			//get model's asset path in this project to place the new prefab next to it 
            string assetPath = AssetDatabase.GetAssetPath(enemyModel.GetInstanceID());
            //e.g. assetPath = "Assets/Models/model.fbx
            //split folder structure for renaming the existing model name as prefab
            string[] folders = assetPath.Split('/');
            //e.g. folders[0] = "Assets", folders[1] = "Models", folders[2] = "model.fbx"
            //then we replace the last part, the model name in folders[2], with the new prefab name
            assetPath = assetPath.Replace(folders[folders.Length-1], enemyModel.name + ".prefab");
            //new asset path: "Assets/Models/model.prefab"
            
            //instantiate, convert and setup model for new prefab
            ProcessModel();

			//if a healthbar prefab is set 
            if (healthbar)
            {
            	//instantiate healthbar prefab
                healthbar = (GameObject)Instantiate(healthbar);
                //rename prefab clone to match naming conventions of Properties.cs
                //remove the "(Clone)" part of the name
                healthbar.name = healthbar.name.Replace("(Clone)", "");
                //parent healthbar to enemy instance
                healthbar.transform.parent = enemyModel.transform;
                //reposition healthbar (Vector3.zero) relative to the enemy
                healthbar.transform.position = enemyModel.transform.position;
            }

            //if a projector prefab is set
            if (projector)
            {
                //instantiate projector prefab
                projector = (GameObject)Instantiate(projector);
                //remove the "(Clone)" part of the name
                projector.name = projector.name.Replace("(Clone)", "");
                //parent projector to enemy instance
                projector.transform.parent = enemyModel.transform;
                //reposition projector (Vector3.zero) relative to the enemy
                projector.transform.position = enemyModel.transform.position;
            }

			//if TweenMove checkbox is checked, attach component
            if (attachTMove)
                enemyModel.AddComponent<TweenMove>();

			//if Properties checkbox is checked
            if (attachProperties)
            {
            	//attach and store Properties component for later use
                Properties properties = enemyModel.AddComponent<Properties>();
                //if we instantiated a healthbar, set it here so the user doesn't have to
                if (healthbar) properties.healthbar = healthbar;
            }

			//if Rigidbody checkbox is checked
            if (attachRigidbody)
            {
            	//attach and store Rigidbody component for later use
                Rigidbody rigidbody = enemyModel.AddComponent<Rigidbody>();
                //disable gravity and kinematic
                rigidbody.useGravity = false;
                rigidbody.isKinematic = false;
            }
			
			//initialize prefab gameobject
            GameObject prefab = null;

			//perform check if we already have a prefab in our project (null if none)
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)))
            {
            	//display custom dialog and wait for user input to overwrite prefab
                if (EditorUtility.DisplayDialog("Are you sure?",
                "The prefab already exists. Do you want to overwrite it?",
                "Yes",
                "No"))
                {
                	//user clicked "Yes", create and overwrite existing prefab
                    prefab = PrefabUtility.CreatePrefab(assetPath, enemyModel);
                }
            }
            else
            	//we haven't created a prefab before nor the project contains one,
            	//create prefab next to the model at assetPath
                prefab = PrefabUtility.CreatePrefab(assetPath, enemyModel);

			//destroy temporary instantiated enemy model in the editor
            DestroyImmediate(enemyModel);
            
            //if we created a prefab
            if (prefab)
            {
            	//select it within the project panel
                Selection.activeGameObject = prefab;
               	//close this editor window
                this.Close();
            }
        }
	}


    void ProcessModel()
    {


    	//temporary instantiate enemy model for creating a prefab of it later
        enemyModel = (GameObject)Instantiate(enemyModel);
        enemyModel.transform.position = Vector3.zero;
        //rename instance name, remove "(Clone)"
        enemyModel.name = enemyModel.name.Replace("(Clone)", "");

		//get all renderers of this model instance to calculate object bounds
		//used to setup the collider
        renderers = enemyModel.GetComponentsInChildren<Renderer>();

		//if the model has no renderer / mesh, debug a warning and skip collider setup
        if (renderers.Length == 0)
            Debug.LogWarning("Enemy Model contains no Renderer! Skipping Collider.");
        else
        {
            /*
        	//for each attached renderer of this enemy model
        	//adjust bounds variable to include all mesh bounds 
            foreach (Renderer renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
             */
        }

        //create empty gameobject as parent and reposition it in the middle
        UnityEngine.GameObject empty = new GameObject(enemyModel.name);
        empty.transform.position = totalBounds.center;
        enemyModel.transform.parent = empty.transform;
        enemyModel = empty;

        //set model instance tag and layer
        enemyModel.tag = tag;
        enemyModel.layer = layer;

        //disable animation autoplay
        Animation anim = enemyModel.GetComponentInChildren<Animation>();
        if(anim != null)
            anim.playAutomatically = false;

        //add a collider with these bounds
        //if(totalBounds.size != Vector3.zero)
            AddCollider();
    }


    void AddCollider()
    {
    	//attach a collider to the model instance depending on the ColliderType selection
        switch (colliderType)
        {
        	//add box collider, reposition center relative to the model instance
        	//set size to calculated bounds and enable trigger
            case ColliderType.boxCollider:
                BoxCollider boxCol = enemyModel.AddComponent<BoxCollider>();
                boxCol.center = totalBounds.center - enemyModel.transform.position;
                boxCol.size = totalBounds.size;
                boxCol.isTrigger = true;
                break;
            //add sphere collider, reposition center relative to the model instance
        	//set radius to calculated bounds width and enable trigger
            case ColliderType.sphereCollider:
                SphereCollider sphereCol = enemyModel.AddComponent<SphereCollider>();
                sphereCol.center = totalBounds.center - enemyModel.transform.position;
                sphereCol.radius = totalBounds.extents.y;
                sphereCol.isTrigger = true;
                break;
            //add capsule collider, reposition center relative to the model instance
        	//set radius to calculated bounds width, height to bounds height, and enable trigger
            case ColliderType.capsuleCollider:
                CapsuleCollider capsuleCol = enemyModel.AddComponent<CapsuleCollider>();
                capsuleCol.center = totalBounds.center - enemyModel.transform.position;
                capsuleCol.radius = totalBounds.extents.x;
                capsuleCol.height = totalBounds.size.y;
                capsuleCol.isTrigger = true;
                break;
            //add mesh collider, reposition center relative to the model instance and enable trigger
        	//we do not automatically adjust the collider here but ask the user to do that instead
        	//because a model could contain multiple meshes and we cannot see which one to use
            case ColliderType.meshCollider:
                MeshCollider meshCol = enemyModel.AddComponent<MeshCollider>();
                meshCol.isTrigger = true;
                Debug.Log("Enemy Setup: Mesh Collider added to " + enemyModel.name + ". Please choose appropriate Mesh.");
                break;
        }
    }
}