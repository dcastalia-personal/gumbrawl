using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class AssetDatabaseUtils {
	
	public static void RegenerateListFromAssets<T>( List<T> existingList, string path ) where T : Object {
		var assets = AssetDatabase.FindAssets($"t:GameObject", new[] { path });
		var foundAssets = (from guid in assets select AssetDatabase.LoadAssetAtPath<T>( AssetDatabase.GUIDToAssetPath( guid ) )).ToList();

		var list = existingList;
		existingList.AddRange( foundAssets.Where( foundAsset => !list.Contains( foundAsset ) ) );
	}
}
