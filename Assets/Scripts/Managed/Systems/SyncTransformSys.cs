using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

public partial struct ExportTransformSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LocalTransform, ExportTransform>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var (localTransform, target) in Query<RefRO<LocalTransform>, RefRO<ExportTransform>>() ) {
			var managedTransform = target.ValueRO.target.Value.transform;
			managedTransform.position = localTransform.ValueRO.Position;
			managedTransform.rotation = localTransform.ValueRO.Rotation;
			var localScale = localTransform.ValueRO.Scale;
			managedTransform.localScale = new Vector3( localScale, localScale, localScale );
		}
	}
}