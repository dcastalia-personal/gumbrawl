using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitCameraRigRefSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadCameraRig, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.TryGetSingleton( out LoadCameraRig loadCameraRig ) ) {
			Object.Instantiate( loadCameraRig.cameraRigPrefab );
		}

		state.EntityManager.DestroyEntity( query );
	}
}

[UpdateInGroup(typeof(InputSysGroup))] [UpdateBefore(typeof(ProcessPointerInput))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SetScreenPosSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ScreenPos, LocalTransform>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		var mainCamera = CameraRig.main.cam;
		foreach( var (screenPos, transform) in Query<RefRW<ScreenPos>, RefRO<LocalTransform>>() ) {
			var screenPt = mainCamera.WorldToScreenPoint( transform.ValueRO.Position );
			screenPos.ValueRW.value = new float2( screenPt.x, screenPt.y );
		}
	}
}