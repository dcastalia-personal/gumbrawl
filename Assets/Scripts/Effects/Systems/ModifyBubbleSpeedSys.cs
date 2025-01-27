using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct ModifyBubbleSpeedSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ModifyBubbleSpeed, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (modifyEffect, parent) in Query<RefRO<ModifyBubbleSpeed>, RefRO<Parent>>().WithAll<RequireInit>() ) {
			var target = GetComponent<EffectTarget>( parent.ValueRO.Value );
			var character = GetComponentRW<Blow>( target.value );
			character.ValueRW.speed = math.clamp( character.ValueRO.speed + modifyEffect.ValueRO.amount, ModifyBubbleSpeed.min, ModifyBubbleSpeed.max );
			
			if( state.WorldUnmanaged.IsServer() ) SetComponentEnabled<Destroy>( parent.ValueRO.Value, true );
		}
	}
}