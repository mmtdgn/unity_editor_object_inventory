using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;

namespace MD.Editor.Tools
{
    public class ObjectInventory : EditorWindow
    {
        private string newCategoryName = "";
        private Vector2 scrollPosition;

        [Serializable]
        public struct InventoryData
        {
            public List<string> Categories;
            public Dictionary<string, List<Object>> Items;
            public int SelectedCategoryIndex;

            public string SelectedCategory => Categories.Count > 0 ? Categories[SelectedCategoryIndex] : null;
        }

        private InventoryData data;

        private string ProjectKey => Application.dataPath;
        private string IndexKey => $"{ProjectKey}_SelectedCategoryIndex";
        private string DataKey => $"{ProjectKey}_ObjectInventoryData";

        [MenuItem("Tools/MD/Asset Inventory")]
        public static void ShowWindow()
        {
            var window = GetWindow<ObjectInventory>();
            window.titleContent = new GUIContent("Asset Inventory");
        }

        private void OnEnable() => LoadInventory();

        /// <summary> Loads inventory data from EditorPrefs. </summary>
        private void LoadInventory()
        {
            data = new InventoryData
            {
                Categories = new List<string>(),
                Items = new Dictionary<string, List<Object>>(),
                SelectedCategoryIndex = EditorPrefs.GetInt(IndexKey, 0)
            };

            string jsonData = EditorPrefs.GetString(DataKey);
            if (string.IsNullOrEmpty(jsonData)) return;

            var loadedData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonData);

            foreach (var entry in loadedData)
            {
                var objects = new List<Object>();
                foreach (var path in entry.Value)
                {
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset != null)
                        objects.Add(asset);
                }

                data.Items[entry.Key] = objects;
                data.Categories.Add(entry.Key);
            }
        }

        /// <summary> Saves inventory data to EditorPrefs. </summary>
        private void SaveInventory()
        {
            var toSerialize = new Dictionary<string, List<string>>();

            foreach (var pair in data.Items)
            {
                var objectPaths = new List<string>();
                foreach (var obj in pair.Value)
                {
                    objectPaths.Add(AssetDatabase.GetAssetPath(obj));
                }

                toSerialize[pair.Key] = objectPaths;
            }

            string json = JsonConvert.SerializeObject(toSerialize);
            EditorPrefs.SetString(DataKey, json);
            EditorPrefs.SetInt(IndexKey, data.SelectedCategoryIndex);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawInventoryArea();
            DrawDragAndDropArea();
            DrawCategoryControls();
        }

        /// <summary> Draws the category selection tabs. </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            for (int i = 0; i < data.Categories.Count; i++)
            {
                GUI.backgroundColor = i == data.SelectedCategoryIndex
                    ? new Color(0.75f, 0.8f, 0.3f, 1f)
                    : new Color(0.9f, 0.55f, 0.55f, 1f);

                GUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
                if (GUILayout.Button(data.Categories[i], EditorStyles.toolbarButton))
                {
                    data.SelectedCategoryIndex = i;
                }

                GUI.backgroundColor = new Color(0.85f, 0.4f, 0.0f, 1f);
                if (GUILayout.Button("X", GUILayout.Width(30f)))
                {
                    RemoveCategoryAtIndex(i);
                    return;
                }

                GUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        /// <summary> Draws the scrollable area containing the selected category's assets. </summary>
        private void DrawInventoryArea()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(EditorStyles.objectFieldThumb);

            if (data.Categories.Count > 0)
            {
                DrawSelectedCategoryAssets();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        /// <summary> Draws the asset fields of the selected category. </summary>
        private void DrawSelectedCategoryAssets()
        {
            var selected = data.SelectedCategory;
            if (string.IsNullOrEmpty(selected) || !data.Items.ContainsKey(selected)) return;

            var list = data.Items[selected];

            for (int i = 0; i < list.Count; i++)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                list[i] = EditorGUILayout.ObjectField(list[i], typeof(Object), false);

                if (list[i] is SceneAsset scene)
                {
                    string path = AssetDatabase.GetAssetPath(scene);
                    DrawSmallButton("Load", () => OpenScene(path));
                }

                if (list[i] is GameObject go)
                {
                    DrawSmallButton("Open", () => AssetDatabase.OpenAsset(go));
                }

                DrawSmallButton("Select", () => Selection.activeObject = list[i]);
                DrawSmallButton("Remove", () => list.RemoveAt(i--));

                GUILayout.EndHorizontal();
            }
        }

        /// <summary> Draws the drag-and-drop area. </summary>
        private void DrawDragAndDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Assets Here", EditorStyles.objectFieldThumb);
            HandleDragAndDrop(dropArea);
        }

        /// <summary> Handles asset drag & drop into the selected category. </summary>
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    evt.Use();

                    if (string.IsNullOrEmpty(data.SelectedCategory)) return;

                    if (!data.Items.ContainsKey(data.SelectedCategory))
                        data.Items[data.SelectedCategory] = new List<Object>();

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (!data.Items[data.SelectedCategory].Contains(obj))
                            data.Items[data.SelectedCategory].Add(obj);
                    }

                    SaveInventory();
                    break;
            }
        }

        /// <summary> Draws category creation, renaming and saving buttons. </summary>
        private void DrawCategoryControls()
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Category Name:", GUILayout.Width(100));
            newCategoryName = EditorGUILayout.TextField(newCategoryName, GUILayout.Height(25));

            DrawLargeButton("Create Category", CreateCategory);

            if (data.Categories.Count > 0)
                DrawLargeButton("Rename Category", RenameCategory);

            DrawLargeButton("Save", () =>
            {
                SaveInventory();
                Debug.Log("Inventory saved.");
            });

            GUILayout.EndHorizontal();
        }

        private void CreateCategory()
        {
            if (string.IsNullOrEmpty(newCategoryName))
            {
                Debug.LogError("Category name cannot be empty.");
                return;
            }

            if (data.Categories.Contains(newCategoryName))
            {
                Debug.LogError("Category already exists.");
                return;
            }

            data.Categories.Add(newCategoryName);
            data.Items[newCategoryName] = new List<Object>();
            data.SelectedCategoryIndex = data.Categories.Count - 1;
            newCategoryName = "";
        }

        private void RenameCategory()
        {
            string current = data.SelectedCategory;

            if (string.IsNullOrEmpty(current) || !data.Items.ContainsKey(current))
                return;

            if (data.Items.ContainsKey(newCategoryName))
            {
                Debug.LogError("Category name already exists.");
                return;
            }

            var assets = data.Items[current];
            data.Items.Remove(current);
            data.Items[newCategoryName] = assets;
            data.Categories[data.SelectedCategoryIndex] = newCategoryName;
            newCategoryName = "";
        }

        private void RemoveCategoryAtIndex(int index)
        {
            if (index < 0 || index >= data.Categories.Count) return;

            string toRemove = data.Categories[index];
            data.Categories.RemoveAt(index);
            data.Items.Remove(toRemove);
            data.SelectedCategoryIndex = Mathf.Clamp(data.SelectedCategoryIndex, 0, data.Categories.Count - 1);
            SaveInventory();
        }

        private void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        private void DrawSmallButton(string label, Action action)
        {
            if (GUILayout.Button(label, GUILayout.Width(60), GUILayout.Height(20)))
            {
                action?.Invoke();
                SaveInventory();
            }
        }

        private void DrawLargeButton(string label, Action action)
        {
            if (GUILayout.Button(label, GUILayout.Height(25)))
            {
                action?.Invoke();
                SaveInventory();
            }
        }
    }
}