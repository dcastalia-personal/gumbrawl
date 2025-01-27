using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitRandomnessSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Randomness, RequireInit>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var randomness in Query<RefRW<Randomness>>().WithAll<RequireInit>() ) {
			randomness.ValueRW.rng = new Random( (uint)UnityEngine.Random.Range( 0, int.MaxValue ) );
		}
	}
}