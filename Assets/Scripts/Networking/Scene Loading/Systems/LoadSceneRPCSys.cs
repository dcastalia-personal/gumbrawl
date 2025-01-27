using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(GhostSimulationSystemGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct LoadSceneOnServerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettings>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SceneFinishedLoadingRPC, ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var matchSettingsEntity = GetSingletonEntity<MatchSettings>();
		var waitingForPlayersToLoadScene = GetComponent<WaitingForPlayersToLoadScene>( matchSettingsEntity );
		
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		// Debug.Log( $"Client ready rpc query on server has {query.CalculateEntityCount()} entites in it" );

		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );

		foreach( var (receiveRpcCommandRequest, self) in Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<SceneFinishedLoadingRPC>().WithEntityAccess() ) {
			waitingForPlayersToLoadScene.numPlayers--;
		}
		
		SetComponent( matchSettingsEntity, waitingForPlayersToLoadScene );

		if( waitingForPlayersToLoadScene.numPlayers > 0 ) return;
		
		var sceneListEntity = GetSingletonEntity<SceneEntry>();
		
		SetComponent( sceneListEntity, new LoadScene { sceneIndex = waitingForPlayersToLoadScene.sceneIndex } );
		SetComponentEnabled<LoadSceneEnabled>( sceneListEntity, true );
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct LoadSceneOnClientSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettings>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadSceneRPC, ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {

		var sceneListEntity = GetSingletonEntity<SceneEntry>();
		var loadSceneReq = query.GetSingleton<LoadSceneRPC>();

		SetComponent( sceneListEntity, new LoadScene { sceneIndex = loadSceneReq.sceneIndex } );
		SetComponentEnabled<LoadSceneEnabled>( sceneListEntity, true );
		
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct UnloadSceneOnClientSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<MatchSettings>();
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<UnloadSceneRPC, ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {

		var sceneListEntity = GetSingletonEntity<SceneEntry>();
		var unloadSceneReq = query.GetSingleton<UnloadSceneRPC>();

		SetComponent( sceneListEntity, new UnloadScene { sceneIndex = unloadSceneReq.sceneIndex } );
		SetComponentEnabled<UnloadSceneEnabled>( sceneListEntity, true );
		
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );
	}
}