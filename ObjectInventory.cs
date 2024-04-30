using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;

namespace S.TD
{
    public class ObjectInventory : EditorWindow
    {
        private string _newCategoryName = "";
        [Serializable]
        public struct ObjectInventoryData
        {
            public List<string> _tabGroups;
            public Dictionary<string, List<Object>> _inventory;
            public int _selectedCategoryIndex;
            public string SelectedCategory => _tabGroups[_selectedCategoryIndex];
        }
        ObjectInventoryData _data;

        [MenuItem("MD/Object Inventory")]
        public static void ShowPanel()
        {
            ObjectInventory wnd = GetWindow<ObjectInventory>();
            wnd.titleContent = new GUIContent("Object Inventory");
        }

        private void OnEnable()
        {
            LoadProperties();
        }

        private void LoadProperties()
        {
            _data = new ObjectInventoryData
            {
                _tabGroups = new List<string>(),
                _inventory = new Dictionary<string, List<Object>>(),
                _selectedCategoryIndex = EditorPrefs.GetInt("SelectedCategoryIndex", 0)
            };

            var _jsonData = EditorPrefs.GetString("ScriptableObjectInventoryData");
            if (!string.IsNullOrEmpty(_jsonData))
            {
                var _inventoryData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(_jsonData);
                foreach (var _inventory in _inventoryData)
                {
                    var _serializedObjects = new List<Object>();
                    foreach (var _serializedObject in _inventory.Value)
                    {
                        var _scriptableObject = AssetDatabase.LoadAssetAtPath<Object>(_serializedObject);
                        _serializedObjects.Add(_scriptableObject);
                    }
                    _data._inventory.Add(_inventory.Key, _serializedObjects);
                    _data._tabGroups.Add(_inventory.Key);
                }
            }
        }

        private void SaveProperties()
        {
            var _inventoryData = new Dictionary<string, List<string>>();
            foreach (var _inventory in _data._inventory)
            {
                var _serializedObjects = new List<string>();
                foreach (var _serializedObject in _inventory.Value)
                {
                    _serializedObjects.Add(AssetDatabase.GetAssetPath(_serializedObject));
                }
                _inventoryData.Add(_inventory.Key, _serializedObjects);
            }

            var _jsonData = JsonConvert.SerializeObject(_inventoryData);
            EditorPrefs.SetString("ScriptableObjectInventoryData", _jsonData);
            EditorPrefs.SetInt("SelectedCategoryIndex", _data._selectedCategoryIndex);
        }

        private void OnGUI()
        {
            // Main area for category tabs
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            _data._selectedCategoryIndex = GUILayout.Toolbar(_data._selectedCategoryIndex, _data._tabGroups.ToArray(), GUILayout.Height(30));
            GUILayout.EndHorizontal();
            DrawSelectedCategory();

            GUILayout.BeginVertical(EditorStyles.helpBox);

            {
                GUILayout.BeginHorizontal();
                CreateButton("Add Element", AddElement, 30f);
                CreateButton("Clear Category", () => _data._inventory[_data.SelectedCategory].Clear(), 30f);
                GUILayout.EndHorizontal();
            }


            // Flexible space to push input field and buttons to the bottom
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Category Name:");
            _newCategoryName = EditorGUILayout.TextField(_newCategoryName);
            CreateButton("Create Category", CreateCategory);
            CreateButton("Remove Category", RemoveCategory);
            GUILayout.EndHorizontal();

            if (GUI.changed)
            {
                SaveProperties();
            }
        }

        private void AddElement()
        {
            if (_data._selectedCategoryIndex < 0 || _data._selectedCategoryIndex >= _data._tabGroups.Count)
            {
                Debug.LogError("Selected category index is out of range");
                return;
            }

            var _selectedGroup = _data._tabGroups[_data._selectedCategoryIndex];
            if (!_data._inventory.ContainsKey(_selectedGroup))
            {
                _data._inventory.Add(_selectedGroup, new List<Object>());
            }
            _data._inventory[_selectedGroup].Add(null);
        }

        private void RemoveCategory()
        {
            if (_data._selectedCategoryIndex >= 0 && _data._selectedCategoryIndex < _data._tabGroups.Count)
            {
                _data._tabGroups.RemoveAt(_data._selectedCategoryIndex);
                _data._selectedCategoryIndex = Mathf.Clamp(_data._selectedCategoryIndex, 0, _data._tabGroups.Count - 1);
            }
        }

        private void CreateButton(string buttonName, Action action, float height = 25f)
        {
            if (GUILayout.Button(buttonName, GUILayout.Height(height)))
            {
                action.Invoke();
            }
        }

        private void DrawSelectedCategory()
        {
            if (_data._selectedCategoryIndex < 0 || _data._selectedCategoryIndex >= _data._tabGroups.Count)
            {
                return;
            }

            var _selectedGroup = _data._tabGroups[_data._selectedCategoryIndex];
            foreach (var _inventory in _data._inventory)
            {
                if (_inventory.Key == _selectedGroup)
                {
                    DrawInventory();
                }
            }
        }

        private void DrawInventory()
        {
            var _serializedObjects = _data._inventory[_data.SelectedCategory];
            if (_serializedObjects == null || _serializedObjects.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _serializedObjects.Count; i++)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                _serializedObjects[i] = EditorGUILayout.ObjectField(_serializedObjects[i], typeof(Object), false);
                if (_serializedObjects[i] is SceneAsset)
                {
                    var _scenePath = AssetDatabase.GetAssetPath(_serializedObjects[i]);
                    CreateButton("Load", () => OpenScene(_scenePath), 20f);
                }
                if (_serializedObjects[i] is GameObject)
                {
                    CreateButton("Open", () => OpenPrefab(_serializedObjects[i]), 20f);
                }
                CreateButton("Select", () => Selection.activeObject = _serializedObjects[i], 20f);
                CreateButton("Remove", () => RemoveInventoryElement(_serializedObjects[i]), 20f);
                GUILayout.EndHorizontal();
            }
        }

        private void OpenPrefab(Object obj)
        {
            AssetDatabase.OpenAsset(obj);
        }

        private void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        private void RemoveInventoryElement(Object serializedObjectPath)
        {
            if (_data._inventory.ContainsKey(_data.SelectedCategory))
            {
                _data._inventory[_data.SelectedCategory].Remove(serializedObjectPath);
            }
        }

        private void CreateCategory()
        {
            if (string.IsNullOrEmpty(_newCategoryName))
            {
                Debug.LogError("Category name is empty");
                return;
            }

            _data._tabGroups.Add(_newCategoryName);
            _newCategoryName = string.Empty; // Clear the input field after adding category
        }
    }
}
