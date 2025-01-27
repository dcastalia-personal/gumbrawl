using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Multiplayer.Playmode;
using Unity.NetCode;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct EnterGameOnClientSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettingsConfig>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<NetworkId>().WithNone<NetworkStreamInGame>() );
		state.RequireForUpdate( query );
	}
	
	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var matchSettingsConfig = GetSingleton<MatchSettingsConfig>();
		var networkConnection = query.GetSingletonEntity();
		
		state.EntityManager.AddComponent<NetworkStreamInGame>( networkConnection );
		var enterGameRPC = state.EntityManager.Instantiate( matchSettingsConfig.enterGameRPC );
		SetComponent( enterGameRPC, new SendRpcCommandRequest { TargetConnection = networkConnection } );

		// Debug.Log( $"Client requesting to enter game via connection {networkConnection}" );
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct EnterGameOnServer : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettings>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<EnterGameReq>().WithAll<ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		
		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );
		
		new EnterGameOnServerJob {
			ecb = ecb,
			matchSettings = GetSingleton<MatchSettings>(),
			linkedEntityLookup = GetBufferLookup<LinkedEntityGroup>(),
			netIdLookup = GetComponentLookup<NetworkId>(),
		}.Schedule( query );

		// Debug.Log( $"Server received request from client to enter game" );
	}
	
	[BurstCompile] partial struct EnterGameOnServerJob : IJobEntity {
		public EntityCommandBuffer ecb;
		public ComponentLookup<NetworkId> netIdLookup;
		public BufferLookup<LinkedEntityGroup> linkedEntityLookup;
		public MatchSettings matchSettings;
		
		void Execute( in ReceiveRpcCommandRequest req ) {
			var reqSource = req.SourceConnection;
			ecb.AddComponent<NetworkStreamInGame>( reqSource );
			var networkId = netIdLookup[ reqSource ];
			
			// UnityEngine.Debug.Log($"'{worldName}' setting connection '{networkId.Value}' to in game, spawning a Ghost '{prefabName}' for them!");

			var newPlayer = ecb.Instantiate( matchSettings.playerPrefab );
			ecb.SetComponent( newPlayer, new GhostOwner { NetworkId = networkId.Value } );
			ecb.AddComponent( reqSource, new PlayerRef { value = newPlayer } );

			// Debug.Log( $"Server created player for network id {networkId.Value}" );

			var linkedEntitiesToConnection = linkedEntityLookup[ req.SourceConnection ];
			linkedEntitiesToConnection.Add( new LinkedEntityGroup { Value = newPlayer } );
		}
	}
}

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitMatchEntryUIForPlayerOnClientSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player, RequireInit>() );
		state.RequireForUpdate( query );
		state.RequireForUpdate<MatchSettingsConfig>();
	}

	public void OnUpdate( ref SystemState state ) {
		// Debug.Log( $"Player initializing on client" );
		var matchSettingsConfig = GetSingleton<MatchSettingsConfig>();
		var matchSettingsUI = matchSettingsConfig.matchSettingsUIInstance.Value;

		#if UNITY_EDITOR
		if( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex > 0 ) {
			// Debug.Log( $"Client aborting UI construction process, since it's been set to automate" );
			return;
		}
		#endif

		foreach( var (name, self) in Query<RefRO<SyncedName>>().WithAll<Player, RequireInit>().WithEntityAccess() ) {
			
			var playerEntryUI = Object.Instantiate( matchSettingsUI.entryUIPrefab, matchSettingsUI.playersLayout );
			playerEntryUI.playerEntity = self;
			matchSettingsUI.playerEntries.Add( playerEntryUI );
			
			playerEntryUI.nameInput.text = name.ValueRO.value.ToString();
		}
		
		if( ClientServerBootstrap.ServerWorld != null ) {
			matchSettingsUI.enterGameButton.gameObject.SetActive( true );
		}
	}
}

// [UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// public partial struct InitMatchSettingsForEntryUISys : ISystem {
//
// 	EntityQuery query;
//
// 	[BurstCompile] public void OnCreate( ref SystemState state ) {
// 		state.RequireForUpdate<MatchSettingsConfig>();
// 		state.RequireForUpdate<MatchSettings>();
// 		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Team, RequireInit>() );
// 		state.RequireForUpdate( query );
// 	}
//
// 	public void OnUpdate( ref SystemState state ) {
// 		var teams = query.GetSingletonBuffer<Team>();
// 		var matchSettingsConfig = GetSingleton<MatchSettingsConfig>();
// 		var matchSettingsUI = matchSettingsConfig.matchSettingsUIInstance.Value;
//
// 		if( matchSettingsUI == null ) return;
//
// 		foreach( var entry in matchSettingsUI.playerEntries ) {
// 			entry.Initialize( teams );
// 			
// 			var player = GetComponent<Player>( entry.playerEntity );
// 			entry.teamDropdown.value = player.teamIndex;
//
// 			Debug.Log( $"Setting player match settings entry to display team value {player.teamIndex} with color {teams[player.teamIndex].color}" );
// 		}
// 	}
// }

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [UpdateAfter(typeof(InitMatchEntryUIForPlayerOnClientSys))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitMatchEntrySettingsForPlayerOnClientSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player, GhostOwnerIsLocal>() );
		state.RequireForUpdate( query );
		state.RequireForUpdate<MatchSettings>();
		state.RequireForUpdate<MatchSettingsConfig>();
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
#if UNITY_EDITOR
		if( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex > 0 ) {
			state.Enabled = false;
			return;
		}
#endif
		
		var matchSettings = GetSingleton<MatchSettings>();
		var matchSettingsConfig = GetSingleton<MatchSettingsConfig>();

		foreach( var (player, self) in Query<RefRO<Player>>().WithAll<GhostOwnerIsLocal>().WithEntityAccess() ) {
			var em = state.EntityManager;
			var numPlayers = QueryBuilder().WithAll<Player>().Build().CalculateEntityCount();
			var savedPlayerName = PlayerPrefs.GetString( "playerName", $"Player {numPlayers}" );

			var playerEntryUI = matchSettingsConfig.matchSettingsUIInstance.Value.playerEntries.First( entry => entry.playerEntity == self );
			
			playerEntryUI.nameInput.text = savedPlayerName;
			UpdatePlayerName( em, savedPlayerName );
				
			playerEntryUI.nameInput.onEndEdit.AddListener( newName => UpdatePlayerName( em, newName ) );
			playerEntryUI.teamDropdown.onValueChanged.AddListener( newTeamIndex => UpdatePlayerTeam( em, (short)newTeamIndex ) );
			
			playerEntryUI.nameInput.interactable = true;
			playerEntryUI.teamDropdown.interactable = true;
		}

		state.Enabled = false;
		
		void UpdatePlayerName( EntityManager em, string newName ) {
			var rpc = em.Instantiate( matchSettings.reqPlayerNameChangeRPC );
			em.SetComponentData( rpc, new ReqPlayerNameChangeRPC { name = newName } );

			PlayerPrefs.SetString( "playerName", newName );
		}

		void UpdatePlayerTeam( EntityManager em, short newTeamIndex ) {
			var rpc = em.Instantiate( matchSettings.reqPlayerTeamChangeRPC );
			em.SetComponentData( rpc, new ReqPlayerTeamChangeRPC { team = newTeamIndex } );
		}
	}
}

#if UNITY_EDITOR

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitAutoPlayerSettingsSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettings>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player, GhostOwnerIsLocal>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var matchSettings = GetSingleton<MatchSettings>();

		int playerIndex = -1;
		if( CurrentPlayer.ReadOnlyTags().Length > 0 && !int.TryParse( CurrentPlayer.ReadOnlyTags()[ 0 ], out playerIndex ) ) {
			Debug.LogWarning( $"There is no index tag associated with player; make sure to add one in the Multiplayer Play Mode settings" );
			return;
		}

		var playerSettings = ConnectLocallyIfOffline.settings.current.playerSettings[ playerIndex ];
		
		// Debug.Log( $"Auto-initializing client with name {playerSettings.name}" );
		
		var nameChangeRpc = state.EntityManager.Instantiate( matchSettings.reqPlayerNameChangeRPC );
		state.EntityManager.SetComponentData( nameChangeRpc, new ReqPlayerNameChangeRPC { name = new FixedString32Bytes( playerSettings.name ) } );
		
		var teamChangeRpc = state.EntityManager.Instantiate( matchSettings.reqPlayerTeamChangeRPC );
		state.EntityManager.SetComponentData( teamChangeRpc, new ReqPlayerTeamChangeRPC { team = (short)playerSettings.team } );

		state.Enabled = false;
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
// [UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitAutoLoadOnServerSys : ISystem {

	EntityQuery query;
	EntityQuery notInGameQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<SceneEntry>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player>() );
		state.RequireForUpdate( query );
		
		notInGameQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithNone<LevelSettings>() );
		state.RequireForUpdate( notInGameQuery );
	}

	public void OnUpdate( ref SystemState state ) {
		if( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex < 1 ) {
			state.Enabled = false;
			return;
		}
		
		if( query.CalculateEntityCount() < ConnectLocallyIfOffline.settings.current.numPlayers ) return;
		
		MatchSettingsUI.EnterGameOnServer( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex );
		state.Enabled = false;
	}
}
#endif

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitPlayerOnClientAndServerSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettings>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player>() );
		state.RequireForUpdate( query );

		state.RequireForUpdate<NetworkId>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {

		// set a reference to the player's connection
		foreach( var (player, owner) in Query<RefRW<Player>, RefRO<GhostOwner>>().WithAll<RequireInit>() ) {
			foreach( var (networkId, self) in Query<RefRO<NetworkId>>().WithEntityAccess() ) {
				if( networkId.ValueRO.Value == owner.ValueRO.NetworkId ) {
					player.ValueRW.connection = self;
					break;
				}
			}
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ReceiveReqChangePlayerNameRPCSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ReqPlayerNameChangeRPC, ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );
		
		new ReceiveChangePlayerNameJob {
			nameLookup = GetComponentLookup<SyncedName>(),
			playerRefLookup = GetComponentLookup<PlayerRef>( isReadOnly: true ),
		}.Schedule( query );
	}

	[BurstCompile] partial struct ReceiveChangePlayerNameJob : IJobEntity {
		public ComponentLookup<SyncedName> nameLookup;
		[ReadOnly] public ComponentLookup<PlayerRef> playerRefLookup;
		
		void Execute( in ReqPlayerNameChangeRPC reqPlayerNameChangeRPC, in ReceiveRpcCommandRequest request ) {
			var playerEntity = playerRefLookup[ request.SourceConnection ].value;
			var name = nameLookup[ playerEntity ];
			name.value = reqPlayerNameChangeRPC.name;
			nameLookup[ playerEntity ] = name;
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ReceiveReqChangePlayerTeamRPCSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ReqPlayerTeamChangeRPC, ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );
		
		new ReceiveChangePlayerTeamJob {
			teamIndexLookup = GetComponentLookup<TeamIndex>(),
			playerRefLookup = GetComponentLookup<PlayerRef>( isReadOnly: true ),
		}.Schedule( query );
	}

	[BurstCompile] partial struct ReceiveChangePlayerTeamJob : IJobEntity {
		public ComponentLookup<TeamIndex> teamIndexLookup;
		[ReadOnly] public ComponentLookup<PlayerRef> playerRefLookup;
		
		void Execute( in ReqPlayerTeamChangeRPC reqPlayerTeamChangeRPC, in ReceiveRpcCommandRequest request ) {
			var playerEntity = playerRefLookup[ request.SourceConnection ].value;
			var teamIndex = teamIndexLookup[ playerEntity ];
			teamIndex.value = reqPlayerTeamChangeRPC.team;
			teamIndexLookup[ playerEntity ] = teamIndex;
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct UpdateMatchSettingsEntryUISys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettingsConfig>();
		state.RequireForUpdate<Team>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var matchSettingsUI = GetSingleton<MatchSettingsConfig>().matchSettingsUIInstance.Value;
		if( matchSettingsUI == null ) return;

		var teams = GetSingletonBuffer<Team>();

		foreach( var (teamIndex, self) in Query<RefRO<TeamIndex>>().WithAll<Player>().WithChangeFilter<TeamIndex>().WithEntityAccess() ) {
			for( int index = 0; index < matchSettingsUI.playersLayout.childCount; index++ ) {
				var entryTransform = matchSettingsUI.playersLayout.GetChild( index );

				if( entryTransform.TryGetComponent( out MatchSettingsEntryUI entry ) ) {
					if( entry.playerEntity != self ) continue;

					if( entry.teamDropdown.options.Count == 0 ) entry.Initialize( teams );
					entry.teamDropdown.value = teamIndex.ValueRO.value;
					entry.teamDropdown.RefreshShownValue();

					break;
				}
			}
		}
		
		foreach( var (name, self) in Query<RefRO<SyncedName>>().WithAll<Player>().WithChangeFilter<SyncedName>().WithEntityAccess() ) {
			for( int index = 0; index < matchSettingsUI.playersLayout.childCount; index++ ) {
				var entryTransform = matchSettingsUI.playersLayout.GetChild( index );

				if( entryTransform.TryGetComponent( out MatchSettingsEntryUI entry ) ) {
					if( entry.playerEntity != self ) continue;
					entry.nameInput.text = name.ValueRO.value.ToString();

					break;
				}
			}
		}
	}
}