using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetSearcher : EditorWindow
{
    UnityEngine.Object newObject = null;
    Dictionary<FieldInfo, ShouldISearchForItAndString> fieldsAndTheirValuesToSearchFor = new Dictionary<FieldInfo, ShouldISearchForItAndString>();
    List<string> valueOfFields = new List<string>();
    List<bool> shouldISearchForBools = new List<bool>();
    System.Type previousType;

    List<SearchResult> searchResults = new List<SearchResult>();

    HashSet<string> scenesToSearch = new HashSet<string>();

    [MenuItem("MoonStudios/AssetSearcher")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AssetSearcher));
    }

    void OnGUI()
    {
        newObject = EditorGUILayout.ObjectField("", newObject, typeof(UnityEngine.Object), true);

        if (newObject is MonoScript)
        {
            MonoScript monoScript = (MonoScript)newObject;
            System.Type monoBehaviourType = monoScript.GetClass();

            if (monoBehaviourType != previousType)
            {
                valueOfFields.Clear();
                shouldISearchForBools.Clear();
                fieldsAndTheirValuesToSearchFor.Clear();
            }

            for (int i = 0; i < monoBehaviourType.GetFields().Length; i++)
            {
                valueOfFields.Add("");
                shouldISearchForBools.Add(false);
                if (!fieldsAndTheirValuesToSearchFor.ContainsKey(monoBehaviourType.GetFields()[i]))
                    fieldsAndTheirValuesToSearchFor.Add(monoBehaviourType.GetFields()[i], new ShouldISearchForItAndString()
                    {
                        shouldISearchForIt = false,
                        value = "",
                    });
            }

            for (int i = 0; i < monoBehaviourType.GetFields().Length; i++)
            {
                FieldInfo field = monoBehaviourType.GetFields()[i];
                GUILayout.BeginHorizontal();
                shouldISearchForBools[i] = EditorGUILayout.Toggle("search for it? ", shouldISearchForBools[i]);
                valueOfFields[i] = EditorGUILayout.TextField(field.Name + " " + field.FieldType.ToString() + " ", valueOfFields[i]);
                fieldsAndTheirValuesToSearchFor[field] = new ShouldISearchForItAndString()
                {
                    shouldISearchForIt = shouldISearchForBools[i],
                    value = valueOfFields[i],
                };
                GUILayout.EndHorizontal();
            }


            previousType = monoBehaviourType;
        }

        if (GUILayout.Button("search"))
        {
            searchResults.Clear();
            Search();
        }
    }

    void Search()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");
        for (int i = 0; i < sceneGuids.Length; i++)
            scenesToSearch.Add(AssetDatabase.GUIDToAssetPath(sceneGuids[i]));

        foreach(string scenePath in scenesToSearch)
        {
            Scene scene = EditorSceneManager.GetSceneByPath(scenePath);

            if (!scene.isLoaded)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            GameObject[] rootGameObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                SearchGameObject(scene.name,rootGameObjects[i]);
            }

            
        }

        //EditorSceneManager.CloseScene(scene, true);
    }

    void SearchGameObject(string sceneName, GameObject go)
    {
        if (newObject is MonoScript)
        {
            SearchMonoScript(sceneName, go);
        }
    }

    void SearchMonoScript(string sceneName, GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component c = components[i];
            if(c is MonoBehaviour)
            {
                MonoBehaviour monoBehaviour = (MonoBehaviour)c;
                MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);
                if (script == newObject)
                {
                    if(!searchResults.Any(x=>x.scene== EditorSceneManager.GetSceneByName(sceneName)))
                    {
                        searchResults.Add(new SearchResult()
                        {
                            scene = EditorSceneManager.GetSceneByName(sceneName),
                            gameObjects=new List<GameObject>(),
                        });

                        SearchResult searchResult = searchResults.Where(x => x.scene == EditorSceneManager.GetSceneByName(sceneName)).FirstOrDefault();
                        searchResult.gameObjects.Add(go);
                    }
                    else
                    {
                        SearchResult searchResult= searchResults.Where(x => x.scene == EditorSceneManager.GetSceneByName(sceneName)).FirstOrDefault();
                        searchResult.gameObjects.Add(go);
                    }
                    
                }
            }
            
        }
    }
}

public class ShouldISearchForItAndString
{
    public bool shouldISearchForIt;
    public object value;
}

public class SearchResult
{
    public Scene scene;
    public List<GameObject> gameObjects = new List<GameObject>();
}