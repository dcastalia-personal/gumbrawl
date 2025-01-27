using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class VictoryUI : MonoBehaviour {
	public Canvas canvas;
	public TMP_Text teamNameLabel;
	public Button nextMatchButton;
	public int levelToLoad;

	public void NextMatch() {
		// TODO: use server world to ask clients to clear out the current level and load it again
		// then do the same once they're finished

		var em = ClientServerBootstrap.ServerWorld.EntityManager;
		var sceneListQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SceneEntry>() );
		if( !sceneListQuery.TryGetSingletonBuffer( out DynamicBuffer<SceneEntry> sceneEntries ) ) return;

		Destroy( InGameUI.current.gameObject );
		SceneSystem.UnloadScene( ClientServerBootstrap.ServerWorld.Unmanaged, sceneEntries[ levelToLoad ].loadedSceneHandle );
		MatchSettingsUI.EnterGameOnServer( (short)levelToLoad );
	}
}
