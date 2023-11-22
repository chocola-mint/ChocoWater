using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CHM.ChocoWater.Editor
{
    internal class InternalUtility
    {
        [MenuItem("GameObject/ChocoWater", false, 1)]
        internal static void CreateChocoWaterPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.chocola-mint.chocowater/Prefabs/ChocoWater.prefab");
            var instance = Object.Instantiate(prefab);
            instance.name = "ChocoWater";
            Undo.RegisterCreatedObjectUndo(instance, "Create ChocoWater");
        }
    }
}
