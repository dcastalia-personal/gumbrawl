using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CharacterStartEatingSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, CharacterInput, Simulate>().WithDisabled<Full, Blowing>().WithPresent<Eating>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new CharacterEatJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct CharacterEatJob : IJobEntity {
		void Execute( in Character character, in CharacterInput input, EnabledRefRW<Eating> eatingEnabled ) {
			if( input.eatPress.IsSet ) {
				eatingEnabled.ValueRW = true;
			}
		}
	}
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CharacterStopEatingSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, CharacterInput, Eating, Simulate>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new CharacterStopEatingJob {}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct CharacterStopEatingJob : IJobEntity {
		void Execute( in Character character, in CharacterInput input, EnabledRefRW<Eating> eatingEnabled ) {
			if( input.eatRelease.IsSet ) {
				eatingEnabled.ValueRW = false;
			}
		}
	}
}

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct EatSys : ISystem {
	EntityQuery query;
	EntityArchetype reqCreateBallProxyArch;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, Eat, Eating, BallIndexRefElement, EffectDisplayReqElement, LocalTransform, Simulate>()
			.WithDisabled<Full>()
			.WithPresent<Chewing>()
			.WithAll<RecentBallProxiesCreated>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new EatJob {
			collisionWorld = GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
			ballLookup = GetComponentLookup<Ball>( isReadOnly: true ),
			predictDestroyLookup = GetComponentLookup<PredictDestroy>(),
			ecb = ecb,
			networkTime = GetSingleton<NetworkTime>(),
			characterEffectRefElementLookup = GetBufferLookup<CharacterEffectRefElement>(),
			bubbleEffectRefElementLookup = GetBufferLookup<BubbleEffectRefElement>(),
			pursueTargetLookup = GetComponentLookup<PursueTarget>(),
			readyToCreateProxyLookup = GetComponentLookup<ProxyRef>(),
		}.Schedule( query );
	}

	[BurstCompile] partial struct EatJob : IJobEntity {
		[ReadOnly] public CollisionWorld collisionWorld;
		[ReadOnly] public ComponentLookup<Ball> ballLookup;
		[ReadOnly] public BufferLookup<CharacterEffectRefElement> characterEffectRefElementLookup;
		public ComponentLookup<ProxyRef> readyToCreateProxyLookup;
		public ComponentLookup<PursueTarget> pursueTargetLookup;
		public BufferLookup<BubbleEffectRefElement> bubbleEffectRefElementLookup;
		public EntityCommandBuffer ecb;
		public NetworkTime networkTime;
		public ComponentLookup<PredictDestroy> predictDestroyLookup;
	    
		void Execute( Entity self, in Eat eat, EnabledRefRW<Eating> eatingEnabled, DynamicBuffer<BallIndexRefElement> ballIndexBuf, DynamicBuffer<EffectDisplayReqElement> effectDisplays,
			EnabledRefRW<Chewing> chewingEnabled, EnabledRefRW<Full> fullEnabled, in LocalTransform transform, DynamicBuffer<RecentBallProxiesCreated> createdProxies ) {
			
			var hits = new NativeList<DistanceHit>( Allocator.Temp );
			collisionWorld.OverlapSphere( transform.Position + eat.mouthOffset, eat.radius, ref hits, eat.canEatFilter );
			int roomLeftInMouth = eat.roomInMouth - ballIndexBuf.Length;
			if( hits.Length > roomLeftInMouth ) hits.Resize( roomLeftInMouth, NativeArrayOptions.UninitializedMemory );

			foreach( var hit in hits ) {
				predictDestroyLookup.SetComponentEnabled( hit.Entity, true );
				pursueTargetLookup[ hit.Entity ] = new PursueTarget { value = self };
				eatingEnabled.ValueRW = false;
				chewingEnabled.ValueRW = true;

				var ball = ballLookup[ hit.Entity ];
				ballIndexBuf.Add( new BallIndexRefElement { value = ball.prefabIndex } );

				if( ballIndexBuf.Length >= eat.roomInMouth ) fullEnabled.ValueRW = true;
				
				if( !networkTime.IsFirstTimeFullyPredictingTick ) return;

				readyToCreateProxyLookup[ hit.Entity ] = new ProxyRef { target = self };
				createdProxies.Add( new RecentBallProxiesCreated { serverIndex = hit.Entity.Index, tick = networkTime.ServerTick } );
				
				// effects
				var characterEffectBuf = characterEffectRefElementLookup[ hit.Entity ];
				foreach( var effect in characterEffectBuf ) {
					var effectInstance = ecb.Instantiate( effect.value );
					ecb.SetComponent( effectInstance, new EffectTarget { value = self } );

					// var effectDisplay = effectDisplayRefLookup[ effect.value ];
					// effectDisplays.Add( new EffectDisplayReqElement { ballPrefabIndex = ball.prefabIndex } );

					// Debug.Log( $"Displaying effect {effect.value}" );
				}

				var bubbleEffectsOnCharacter = bubbleEffectRefElementLookup[ self ];
				var bubbleEffectsOnBall = bubbleEffectRefElementLookup[ hit.Entity ];
				bubbleEffectsOnCharacter.AddRange( bubbleEffectsOnBall.ToNativeArray( Allocator.Temp ) );

				for( int index = 0; index < bubbleEffectsOnCharacter.Length; index++ ) {
					BubbleEffectRefElement effect = bubbleEffectsOnCharacter[ index ];
					bubbleEffectsOnCharacter[ index ] = effect;
				}
				
				effectDisplays.Add( new EffectDisplayReqElement { ballPrefabIndex = ball.prefabIndex } );
				
				// Debug.Log( $"Eating entity {hit.Entity} with {characterEffectBuf.Length} character effects and {bubbleEffectsOnBall.Length} bubble effects" );
			}
		}
	}
}

public partial struct CleanupRecentProxiesBufferSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<RecentBallProxiesCreated>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new CleanupRecentProxiesBufferJob {
		    networkTime = GetSingleton<NetworkTime>(),
		    fixedDeltaTime = SystemAPI.Time.fixedDeltaTime,
		}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct CleanupRecentProxiesBufferJob : IJobEntity {
		public NetworkTime networkTime;
		public float fixedDeltaTime;
		const float bufferDuration = 1f;
	    
		void Execute( DynamicBuffer<RecentBallProxiesCreated> recentBallProxiesCreated ) {
			for( int index = recentBallProxiesCreated.Length - 1; index > -1; index-- ) {
				var tickDifference = networkTime.ServerTick.TicksSince( recentBallProxiesCreated[ index ].tick );

				if( tickDifference * fixedDeltaTime > bufferDuration ) {
					recentBallProxiesCreated.RemoveRangeSwapBack( 0, index + 1 );
					break;
				}
			}
		}
	}
}