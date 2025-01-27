using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct SpawnPrefabSys : ISystem {
	EntityQuery query;
	EntityQuery ballListInitializedQuery;
	
	public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<SpawnPrefab, LocalTransform, Randomness>() );
		state.RequireForUpdate( query );
		
		ballListInitializedQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<BallsList>().WithNone<RequireInit>() );
		state.RequireForUpdate( ballListInitializedQuery );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new SpawnPrefabJob {
		    ecb = ecb,
		    deltaTime = SystemAPI.Time.DeltaTime,
		    ballPrefabs = GetSingletonBuffer<BallPrefabRef>()
		}.Schedule( query );
	}

	[BurstCompile] partial struct SpawnPrefabJob : IJobEntity {
		[ReadOnly] public DynamicBuffer<BallPrefabRef> ballPrefabs;
		public EntityCommandBuffer ecb;
		public float deltaTime;
	    
		void Execute( ref SpawnPrefab spawnPrefab, in LocalTransform transform, ref Randomness randomness ) {
			spawnPrefab.timer -= deltaTime;
			spawnPrefab.frequency = math.clamp( spawnPrefab.frequency - spawnPrefab.rateIncreasePerSecond * deltaTime, 2f, 100f );

			if( spawnPrefab.timer > 0f ) return;
			
			var randomIndex = randomness.rng.NextInt( 0, ballPrefabs.Length );
			var instance = ecb.Instantiate( ballPrefabs[ randomIndex ].value );
			ecb.SetComponent( instance, transform );

			var speed = randomness.rng.NextFloat( spawnPrefab.speedRange.x, spawnPrefab.speedRange.y );
			ecb.SetComponent( instance, new PhysicsVelocity {
				Linear = math.normalize( randomness.rng.NextFloat3( new float3( -1f, -1f, 0f ), new float3( 1f, 1f, 0f ) ) ) * speed
			} );
			ecb.SetComponent( instance, new ConstantSpeed { value = speed } );

			spawnPrefab.timer = spawnPrefab.frequency;
		}
	}
}

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitBallColorSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Ball, BaseColor, RequireInit>() );
		state.RequireForUpdate( query );
		state.RequireForUpdate<BallColor>();
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitBallColorJob {
			ballsList = GetSingletonBuffer<BallColor>( isReadOnly: true ),
		}.Schedule( query );
	}

	[BurstCompile] partial struct InitBallColorJob : IJobEntity {
		[ReadOnly] public DynamicBuffer<BallColor> ballsList;
		
		void Execute( ref BaseColor baseColor, in Ball ball ) {
			// Debug.Log( $"Trying to set ball color on prefab index {ball.prefabIndex}" );
			baseColor.value = ballsList[ ball.prefabIndex ].value;
		}
	}
}