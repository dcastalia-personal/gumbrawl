using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Serialization;

public class MatchSettingsAuth : MonoBehaviour {
	public GameObject playerPrefab;
	public GameObject reqPlayerNameChangeRPC;
	public GameObject reqPlayerTeamChangeRPC;
	public GameObject loadSceneReq; // by default, set to the "first level" of an in-game scene
	public GameObject unloadSceneReq;
	
	public class Baker : Baker<MatchSettingsAuth> {

		public override void Bake( MatchSettingsAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MatchSettings {
				playerPrefab = GetEntity( auth.playerPrefab, TransformUsageFlags.None ),
				reqPlayerNameChangeRPC = GetEntity( auth.reqPlayerNameChangeRPC, TransformUsageFlags.None ),
				reqPlayerTeamChangeRPC = GetEntity( auth.reqPlayerTeamChangeRPC, TransformUsageFlags.None ),
				loadSceneReq = GetEntity( auth.loadSceneReq, TransformUsageFlags.None ),
			} );

			AddComponent( self, new WaitingForPlayersToLoadScene {} ); SetComponentEnabled<WaitingForPlayersToLoadScene>( self, false );
			AddComponent( self, new WaitForDisconnect {} ); SetComponentEnabled<WaitForDisconnect>( self, false );
		}
	}
}

public struct MatchSettings : IComponentData {
	public Entity reqPlayerNameChangeRPC;
	public Entity reqPlayerTeamChangeRPC;
	public Entity loadSceneReq;
	public Entity unloadSceneReq;
	public Entity playerPrefab;
}

public struct WaitingForPlayersToLoadScene : IComponentData, IEnableableComponent {
	public int sceneIndex;
	public int numPlayers;
}

public struct WaitForDisconnect : IComponentData, IEnableableComponent {
	
}