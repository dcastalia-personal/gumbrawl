using Networking.Connection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitMatchSettingsUIOnClientSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchSettingsConfig, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		// Debug.Log( $"Match settings config initializing on client" );
		
#if UNITY_EDITOR
		if( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex > 0 ) {
			ConnectorSys.DestroyLocalSimulationWorld();
			return;
		}
#endif

		foreach( var matchSettingsConfig in Query<RefRW<MatchSettingsConfig>>().WithAll<RequireInit>() ) {
			
			var matchSettingsGameObject = Object.Instantiate( matchSettingsConfig.ValueRO.matchSettingsUIPrefab ) as GameObject;
			var matchSettingsUI = matchSettingsGameObject!.GetComponent<MatchSettingsUI>();
			matchSettingsConfig.ValueRW.matchSettingsUIInstance = matchSettingsUI;

			if( ClientServerBootstrap.ServerWorld != null ) {
				var serverEM = ClientServerBootstrap.ServerWorld.EntityManager;
				var joinCodeQuery = serverEM.CreateEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<JoinCode>() );
				if( joinCodeQuery.TryGetSingleton( out JoinCode joinCode ) ) {
					matchSettingsUI.joinCodeRoot.SetActive( true );
					matchSettingsUI.joinCodeLabel.text = joinCode.Value.ToString();
				}
			}
		}
		
		ConnectorSys.DestroyLocalSimulationWorld();
	}
}

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InitMatchSettingsUIOnServerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchSettingsConfig, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		var matchSettingsConfig = GetSingleton<MatchSettingsConfig>();
		ecb.Instantiate( matchSettingsConfig.matchSettingsPrefab );
		
		ConnectorSys.DestroyLocalSimulationWorld();
	}
}

public partial struct CleanupMatchSettingsConfigInGameSys : ISystem {
	EntityQuery matchSettingsQuery;
	EntityQuery inGameQuery;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		matchSettingsQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchSettingsConfig>() );
		state.RequireForUpdate( matchSettingsQuery );
		
		inGameQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings>() );
		state.RequireForUpdate( inGameQuery );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var matchSettingsConfigEntity = matchSettingsQuery.GetSingletonEntity();
		SetComponentEnabled<Destroy>( matchSettingsConfigEntity, true );
	}
}

[UpdateInGroup(typeof(CleanupGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CleanupMatchSettingsUISys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<MatchSettingsConfig, Destroy>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		var matchSettingsConfig = query.GetSingleton<MatchSettingsConfig>();
		if( matchSettingsConfig.matchSettingsUIInstance.Value != null ) Object.Destroy( matchSettingsConfig.matchSettingsUIInstance.Value.gameObject ); // can be null if you skipped the menu
	}
}