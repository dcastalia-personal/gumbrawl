using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitBubbleHeavyEffectSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<NextBubbleHeavyEffect, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (modification, parent) in Query<RefRO<NextBubbleHeavyEffect>, RefRO<Parent>>().WithAll<RequireInit>() ) {
			var target = GetComponent<EffectTarget>( parent.ValueRO.Value );

			var gravityFactor = GetComponentRW<PhysicsGravityFactor>( target.value );
			gravityFactor.ValueRW.Value = math.clamp( gravityFactor.ValueRO.Value + modification.ValueRO.modifyGravityScale, NextBubbleHeavyEffect.min, NextBubbleHeavyEffect.max );

			if( state.WorldUnmanaged.IsServer() ) SetComponentEnabled<Destroy>( parent.ValueRO.Value, true );
		}
	}
}