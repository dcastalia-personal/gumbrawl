using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitInGameOnClientSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		// Debug.Log( $"Client finished loading in game, so level settings that require init are present" );
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new InitInGameOnClientSysJob {
			ecb = ecb,
		}.Schedule( query );
	}

	[BurstCompile] partial struct InitInGameOnClientSysJob : IJobEntity {
		public EntityCommandBuffer ecb;
		
		void Execute( in LevelSettings levelSettingsOnClient ) {
			ecb.Instantiate( levelSettingsOnClient.finishedEnteringGameRpc );
			// Debug.Log( $"Client sending ready rpc to server" );
		}
	}
}