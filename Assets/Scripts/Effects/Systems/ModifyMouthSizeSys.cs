using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct ModifyMouthSizeSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ModifyMouthSize, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		foreach( var (modifyEffect, parent) in Query<RefRO<ModifyMouthSize>, RefRO<Parent>>().WithAll<RequireInit>() ) {
			var target = GetComponent<EffectTarget>( parent.ValueRO.Value );
			var character = GetComponentRW<Eat>( target.value );
			character.ValueRW.roomInMouth = math.clamp( character.ValueRO.roomInMouth + modifyEffect.ValueRO.amount, ModifyMouthSize.min, ModifyMouthSize.max );
			
			if( state.WorldUnmanaged.IsServer() ) SetComponentEnabled<Destroy>( parent.ValueRO.Value, true );
		}
	}
}