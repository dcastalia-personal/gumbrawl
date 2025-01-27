using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CharacterMovementSys : ISystem {
	public const float baselineSpeed = 1f;
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, CharacterInput, PhysicsVelocity, GhostOwnerIsLocal, Simulate>().WithDisabled<Blowing, Stunned>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var myCharacterQuery = QueryBuilder().WithAll<Character, PhysicsVelocity, GhostOwnerIsLocal, Simulate>().Build();
		if( myCharacterQuery.IsEmpty ) return;
		
		var clearCharacterMovementJob = new ClearCharacterMovementJob {}.Schedule( myCharacterQuery, state.Dependency );
		state.Dependency = new CharacterMovementJob {}.Schedule( query, clearCharacterMovementJob );
	}

	[BurstCompile] partial struct ClearCharacterMovementJob : IJobEntity {
		void Execute( in Character character, ref PhysicsVelocity velocity ) {
			velocity = default;
		}
	}

	[BurstCompile] partial struct CharacterMovementJob : IJobEntity {
		
		void Execute( in Character character, in CharacterInput characterInput, ref PhysicsVelocity velocity ) {
			var travelVelocity = new float3( characterInput.movementDelta.x, characterInput.movementDelta.y, 0f ) * baselineSpeed * character.speed;
			velocity.Linear = travelVelocity;
		}
	}
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct StunnedCountdownSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StunnedTime, Stunned, Simulate>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new StunnedCountdownJob {
		    deltaTime = SystemAPI.Time.DeltaTime
		}.Schedule( query );
	}

	[BurstCompile] partial struct StunnedCountdownJob : IJobEntity {
		public float deltaTime;
	    
		void Execute( ref StunnedTime stunnedTime, EnabledRefRW<Stunned> stunned ) {
			stunnedTime.time -= deltaTime;

			if( stunnedTime.time < 0f ) {
				stunned.ValueRW = false;
			}
		}
	}
}