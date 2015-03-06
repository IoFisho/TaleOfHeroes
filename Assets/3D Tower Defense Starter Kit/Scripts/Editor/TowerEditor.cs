/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//TowerEditor.cs displays a custom tower setting interface for TowerManager.cs
[CustomEditor(typeof(TowerManager))]
public class TowerEditor : Editor
{
    private TowerManager script;    //TowerManager reference
    private string newName = "";    //tower name placeholder

    //display custom inspector
    public override void OnInspectorGUI()
    {
        //get manager reference
        script = (TowerManager)target;  
		
		//set the targeted script modified by the GUI for handling undo
        Undo.SetSnapshotTarget(script, "Changed Settings");
        //save the current state of all objects set with SetSnapshotTarget to internal snapshot
        Undo.CreateSnapshot();
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("New Tower Name:");
        //tower name input
        this.newName = EditorGUILayout.TextField(this.newName);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        //add a new row to config a new tower
        if (GUILayout.Button("Add Tower!"))
        {
            //don't continue if no name is set
            if (newName == null || newName == "")
            {
                Debug.LogWarning("Tower Manager: no Tower Name typed in - aborting.");
                return;
            }

            //add name to list
            script.towerNames.Add(newName);
            //add null placeholder to prefab list
            script.towerPrefabs.Add(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Sell Loss %");
        //sell loss inspector input
        script.sellLoss = EditorGUILayout.IntField(script.sellLoss);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Name:");
        GUILayout.Label("Prefab:");
        EditorGUILayout.EndHorizontal();

        //don't continue if no tower is set (don't repaint inspector)
        if (script.towerNames.Count == 0) return;

        //display configurable row for each tower in inspector
        for (int i = 0; i < script.towerNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            //display name ( editable text field )
            script.towerNames[i] = EditorGUILayout.TextField(script.towerNames[i]);
            //prefab slot
            script.towerPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(script.towerPrefabs[i], typeof(GameObject), false);

            //button to remove the selected row
            if (GUILayout.Button("X"))
            {
                //remove this specific row - name, prefab and button texture from list
                script.towerNames.RemoveAt(i);
                script.towerPrefabs.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }
		
		if (GUI.changed)
        {
            //we have to tell Unity that a value of the TowerManager script has changed
            //http://unity3d.com/support/documentation/ScriptReference/EditorUtility.SetDirty.html
            EditorUtility.SetDirty(script);
            //Register the snapshot state made with CreateSnapshot so the user can later undo back to that state
            Undo.RegisterSnapshot();
            //repaint editor GUI window
            Repaint();
        }
        else
            //clear the snapshot at end of call
            Undo.ClearSnapshotTarget();
    }
}
