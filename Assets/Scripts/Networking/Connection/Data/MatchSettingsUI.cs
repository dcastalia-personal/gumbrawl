using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UI;

public class MatchSettingsUI : MonoBehaviour {
	public RectTransform playersLayout;
	public MatchSettingsEntryUI entryUIPrefab;

	public GameObject joinCodeRoot;
	public TMP_Text joinCodeLabel;

	public List<MatchSettingsEntryUI> playerEntries = new();

	public Button enterGameButton;

	public void EnterGame() {
		EnterGameOnServer();
	}

	public static void EnterGameOnServer( short level = -1 ) {
		var em = ClientServerBootstrap.ServerWorld.EntityManager;
		var matchSettingsQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchSettings>() );
		if( !matchSettingsQuery.TryGetSingletonEntity<MatchSettings>( out Entity matchSettingsEntity ) ) return;
		var matchSettings = em.GetComponentData<MatchSettings>( matchSettingsEntity );
		var waitingForPlayers = em.GetComponentData<WaitingForPlayersToLoadScene>( matchSettingsEntity );

		// var sceneListQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SceneEntry>() );

		// Debug.Log( $"Server trying to get load scene rpc request from entity {matchSettings.loadSceneReq}" );
		
		var loadSceneReq = em.GetComponentData<LoadSceneRPC>( matchSettings.loadSceneReq );
		var loadSceneReqEntity = em.Instantiate( matchSettings.loadSceneReq );

		if( level != -1 ) {
			loadSceneReq.sceneIndex = level;
			em.SetComponentData( loadSceneReqEntity, loadSceneReq );
		}

		var playersQuery = em.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player>() );
		waitingForPlayers.numPlayers = playersQuery.CalculateEntityCount();
#if UNITY_EDITOR
		if( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex > 0 ) waitingForPlayers.numPlayers = ConnectLocallyIfOffline.settings.current.numPlayers;
#endif
		waitingForPlayers.sceneIndex = loadSceneReq.sceneIndex;
		em.SetComponentData( matchSettingsEntity, waitingForPlayers );

		// var sceneListEntity = sceneListQuery.GetSingletonEntity();
		// em.SetComponentData( sceneListEntity, new LoadScene { sceneToLoad = loadSceneReq.sceneIndex } );
		// em.SetComponentEnabled<LoadSceneEnabled>( sceneListEntity, true );
	}
}
