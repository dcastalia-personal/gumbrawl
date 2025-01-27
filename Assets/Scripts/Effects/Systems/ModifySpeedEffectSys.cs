using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitModifySpeedEffectSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ModifySpeedEffect, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (modifySpeedEffect, parent) in Query<RefRO<ModifySpeedEffect>, RefRO<Parent>>().WithAll<RequireInit>() ) {
			var target = GetComponent<EffectTarget>( parent.ValueRO.Value );
			var character = GetComponentRW<Character>( target.value );
			character.ValueRW.speed = math.clamp( character.ValueRO.speed + modifySpeedEffect.ValueRO.amount, ModifySpeedEffect.min, ModifySpeedEffect.max );
			
			if( state.WorldUnmanaged.IsServer() ) SetComponentEnabled<Destroy>( parent.ValueRO.Value, true );
		}
	}
}