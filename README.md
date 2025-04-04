# 🧰 Unity Editor Tool: Asset Inventory

**Author:** Mehmet Doğan  
**Menu Path:** `Tools > MD > Asset Inventory`

---

## 🔍 Overview
**Asset Inventory** is a custom Unity Editor tool designed to help you organize project assets into categories. It provides an intuitive drag-and-drop interface, making it easier to group, access, and manage your prefabs, scenes, scripts, and other Unity assets without manually searching through folders.

---

| Inventory Tool | Example |
|----|---|
|<img src="/Resources/Icon.png">|<img src="/Resources/favorites.png">|

---

## 🧠 Features

| Feature                     | Description                                                                 |
|-----------------------------|-----------------------------------------------------------------------------|
| ✅ Category Management       | Create, rename, or delete asset categories.                                |
| ✅ Asset Listing             | Display and manage assets assigned to the selected category.               |
| ✅ Drag & Drop Support       | Easily drag assets into the selected category.                             |
| ✅ Scene Quick Load          | Load scenes directly from the inventory.                                   |
| ✅ Object Quick Access       | Select or open assets directly from the tool.                              |
| ✅ Data Persistence          | Automatically saves data using `EditorPrefs` with JSON serialization.      |

---

## 🧪 How It Works

### 📂 Categories
- **Creation**: Type a name and click **Create Category**.
- **Selection**: Click on any tab to view assets under that category.
- **Renaming**: Use the rename button to change the selected category’s name.
- **Deletion**: Click the **X** button on the tab to remove a category.

### 📦 Inventory Panel
- Displays a scrollable list of all assets under the currently selected category.
- Each asset has buttons to:
  - **Load** (for scenes)
  - **Open** (for GameObjects)
  - **Select** (highlight in project)
  - **Remove** (delete from inventory)

### 🎯 Drag & Drop Area
- Drag and drop any asset from the project window.
- Assets are added to the currently selected category.
- Prevents duplicate entries.

---

## 💾 Saving and Loading
- Inventory data is saved using `EditorPrefs` under a unique project-based key.
- Assets are stored by their paths and reloaded on startup using `AssetDatabase.LoadAssetAtPath`.
- Data is saved whenever a change is made (e.g., adding/removing assets, switching category).

---

## 🧬 Code Structure Summary

| Component               | Purpose                                           |
|------------------------|----------------------------------------------------|
| `InventoryData`        | Holds the categories, assets, and selected index. |
| `LoadInventory()`      | Reads saved data from `EditorPrefs`.              |
| `SaveInventory()`      | Serializes current inventory data to `EditorPrefs`.|
| `DrawToolbar()`        | Renders category tabs with delete functionality.  |
| `DrawInventoryArea()`  | Displays the asset list of the selected category. |
| `DrawDragAndDropArea()`| Allows adding assets via drag and drop.           |
| `DrawCategoryControls()`| Category creation, renaming, and saving.         |
| `DrawSmallButton()`    | Utility function for asset action buttons.        |
| `DrawLargeButton()`    | Utility function for category action buttons.     |

---

## 🧭 Tips
- Ideal for organizing large projects with many assets.
- Helps teams manage assets by categories like “Enemies,” “UI,” “Scenes,” “VFX,” etc.
- Integrates seamlessly into the Unity Editor, no additional setup required.
