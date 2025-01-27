using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct ConstantSpeedSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ConstantSpeed, PhysicsVelocity>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new ConstantSpeedJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct ConstantSpeedJob : IJobEntity {
	    
		void Execute( in ConstantSpeed constantSpeed, ref PhysicsVelocity velocity ) {
			velocity.Linear = math.normalize( velocity.Linear ) * constantSpeed.value;
		}
	}
}