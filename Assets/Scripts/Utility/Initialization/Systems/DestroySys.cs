using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(EndSimulationSyncGroup), OrderLast = true)]
public partial struct DestroySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Destroy>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var destructionCandidates = query.ToEntityArray( Allocator.Temp );
		state.EntityManager.DestroyEntity( destructionCandidates );
	}
}

[UpdateInGroup(typeof(CleanupGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct HideIfPredictedToDestroyOnClientSys : ISystem {
	EntityQuery query;
	EntityQuery refQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PredictDestroy>().WithPresent<MaterialMeshInfo>() );
		query.AddChangedVersionFilter( ComponentType.ReadOnly<PredictDestroy>() );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new HideIfPredictedToDestroyJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct HideIfPredictedToDestroyJob : IJobEntity {
		void Execute( EnabledRefRO<PredictDestroy> predictDestroyEnabled, EnabledRefRW<MaterialMeshInfo> rendererEnabled ) {
			rendererEnabled.ValueRW = !predictDestroyEnabled.ValueRO;
		}
	}
}

[UpdateInGroup(typeof(CleanupGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PredictDestroyOnServerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PredictDestroy>().WithDisabled<Destroy>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		new PredictDestroyOnServerJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct PredictDestroyOnServerJob : IJobEntity {
		void Execute( Entity self, EnabledRefRW<Destroy> destroyEnabled ) {
			destroyEnabled.ValueRW = true;
		}
	}
}