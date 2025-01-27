using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ConnectLocallyIfOffline : MonoBehaviour {
	
	#if UNITY_EDITOR

	public SceneListAuth sceneList;
	public SceneAsset startScene;
	[FormerlySerializedAs( "debugPlayModeSettings" )] public DebugPlayModeSettings current;
	public static ConnectLocallyIfOffline settings;
	
	// will only execute if starting in an unbaked scene in Unity
	void Awake() {
		if( settings != null ) {
			Destroy( this.gameObject );
			return;
		}

		settings = this;
		DontDestroyOnLoad( this.gameObject );
		
		var currentScene = SceneManager.GetActiveScene();

		for( short index = 0; index < sceneList.scenes.Count; index++ ) {
			SceneAsset sceneAsset = sceneList.scenes[ index ];
			// Debug.Log( $"Comparing starting scene {currentScene.name} to {sceneAsset.name}" );
			if( !string.Equals( sceneAsset.name, currentScene.name, StringComparison.Ordinal ) ) continue;
			
			current.overrideStartSceneIndex = index;
			break;
		}

		// Debug.Log( $"Start scene index set to {current.overrideStartSceneIndex}" );
		if( current.overrideStartSceneIndex == -1 ) return;
		
		SceneManager.LoadScene( startScene.name );
	}
	
	#endif
}

#if UNITY_EDITOR
// use these to automatically configure multiplayer settings and skip the UIs

[Serializable]
public class DebugPlayModeSettings {
	public short overrideStartSceneIndex = -1;

	[Range(1, 5)] public int numPlayers = 1;

	public List<DebugPlayerSettings> playerSettings = new();
}

[Serializable]
public class DebugPlayerSettings {
	public string name;
	public int team;
}
#endif