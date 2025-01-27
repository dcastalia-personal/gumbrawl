using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.SystemAPI;

// [UpdateInGroup(typeof(InitAfterSceneSysGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
// public partial struct InitLevelSingletonsOnServerSys : ISystem {
// 	EntityQuery query;
//
// 	[BurstCompile] public void OnCreate( ref SystemState state ) {
// 		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings, RequireInit>() );
// 		state.RequireForUpdate( query );
// 	}
//
// 	[BurstCompile] public void OnUpdate( ref SystemState state ) {
// 		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
// 		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
//
// 		foreach( var levelSettings in Query<RefRO<LevelSettings>>().WithAll<RequireInit>() ) {
// 			ecb.Instantiate( levelSettings.ValueRO.scoringPrefab );
// 			ecb.Instantiate( levelSettings.ValueRO.ballListPrefab );
// 		}
// 	}
// }

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InitLevelSingletonsOnServerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		foreach( var levelSettings in Query<RefRO<LevelSettings>>().WithAll<RequireInit>() ) {
			ecb.Instantiate( levelSettings.ValueRO.ballsListPrefab );
			ecb.Instantiate( levelSettings.ValueRO.scoringPrefab );
		}
	}
}

[UpdateInGroup(typeof(InitAfterSceneSysGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitLevelSingletonsOnClientSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var levelSettings in Query<RefRO<LevelSettings>>().WithAll<RequireInit>() ) {
			if( InGameUI.current ) Object.Destroy( InGameUI.current.gameObject );
			
			var inGameUIObject = (GameObject)Object.Instantiate( levelSettings.ValueRO.inGameUIPrefab );
			InGameUI.current = inGameUIObject.GetComponent<InGameUI>();
			
			Object.Instantiate( levelSettings.ValueRO.managedPoolPrefab );

			// Debug.Log( $"Initializing UI" );
		}
	}
}

[UpdateInGroup(typeof(CleanupGroup))]
public partial struct CleanupLevelSingletonsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings, Destroy>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var levelSettings in Query<RefRO<LevelSettings>>().WithAll<Destroy>() ) {
			Object.Destroy( InGameUI.current.gameObject );
			Object.Destroy( Pool.current.gameObject );
		}
	}
}