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
public partial struct InitBubbleWorthEffectSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<NextBubbleWorthEffect, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (modification, parent) in Query<RefRO<NextBubbleWorthEffect>, RefRO<Parent>>().WithAll<RequireInit>() ) {
			var target = GetComponent<EffectTarget>( parent.ValueRO.Value );

			var worth = GetComponentRW<WorthPoints>( target.value );
			worth.ValueRW.value = (short)math.clamp( worth.ValueRO.value + modification.ValueRO.modification, NextBubbleWorthEffect.min, NextBubbleWorthEffect.max );

			if( state.WorldUnmanaged.IsServer() ) SetComponentEnabled<Destroy>( parent.ValueRO.Value, true );
		}
	}
}