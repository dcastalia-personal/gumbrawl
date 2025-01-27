using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitBubbleSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, BaseColor, TeamIndexLocal, CharacterRef, PlayerRef, RequireInit>().WithAll<Speed>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitBubbleJob {
			playerRefLookup = GetComponentLookup<PlayerRef>(),
			teams = GetSingletonBuffer<Team>(),
			blowLookup = GetComponentLookup<Blow>(),
			teamIndexLookup = GetComponentLookup<TeamIndex>(),
		}.Schedule( query );
	}
}

[BurstCompile] partial struct InitBubbleJob : IJobEntity {
	public ComponentLookup<PlayerRef> playerRefLookup;
	[ReadOnly] public ComponentLookup<Blow> blowLookup;
	[ReadOnly] public DynamicBuffer<Team> teams;
	[ReadOnly] public ComponentLookup<TeamIndex> teamIndexLookup;
	const float bubbleAlpha = 0.9f;
		
	void Execute( Entity self, ref BaseColor color, in CharacterRef characterRef, ref TeamIndexLocal teamIndex, ref Speed speed ) {
		var playerRef = playerRefLookup[ characterRef.value ];
		playerRefLookup[ self ] = playerRef;

		var playerTeamIndex = teamIndexLookup[ playerRef.value ];
		teamIndex = new TeamIndexLocal { value = playerTeamIndex.value };
			
		var team = teams[ teamIndex.value ];
		color.value = team.color;
		color.value.w = bubbleAlpha;
		speed.value = blowLookup[ characterRef.value ].speed;
	}
}

[UpdateInGroup(typeof(InitGhostSystemGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct InitBubbleAfterGhostSpawnSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, BaseColor, TeamIndexLocal, CharacterRef, PlayerRef, RequireInit>().WithAll<Speed>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitBubbleJob {
			playerRefLookup = GetComponentLookup<PlayerRef>(),
			teams = GetSingletonBuffer<Team>(),
			blowLookup = GetComponentLookup<Blow>(),
			teamIndexLookup = GetComponentLookup<TeamIndex>(),
		}.Schedule( query );
	}
}

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
public partial struct ExpandBubbleSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, Speed, LocalTransform, SpawnTick, Blowing, Simulate>().WithPresent<GhostOwnerIsLocal>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		// var shouldBreak = new NativeReference<bool>( Allocator.TempJob );
		new ExpandBubbleJob {
			networkTime = GetSingleton<NetworkTime>(),
			deltaTime = SystemAPI.Time.fixedDeltaTime,
			// isServer = state.WorldUnmanaged.IsServer(),
			// shouldBreak = shouldBreak,
		}.Schedule( query );

		// expandBubbleJob.Complete();

		// if( shouldBreak.Value ) Debug.Break();
	}

	[BurstCompile] partial struct ExpandBubbleJob : IJobEntity {
		public float deltaTime;
		public NetworkTime networkTime;
		public bool isServer;
		// public NativeReference<bool> shouldBreak;
		
		void Execute( in Speed speed, in SpawnTick spawnTick, ref LocalTransform transform, EnabledRefRO<GhostOwnerIsLocal> ghostOwnerIsLocal ) {
			var tickToUse = ghostOwnerIsLocal.ValueRO ? networkTime.ServerTick : networkTime.InterpolationTick;
			var ticksAlive = tickToUse.TicksSince( spawnTick.value );
			transform.Scale = ticksAlive * deltaTime * speed.value;

			// if( !isServer ) {
			// 	Debug.Log( $"Expanding bubble to scale {transform.Scale} based on ticks alive {ticksAlive}, delta time {deltaTime} and speed {speed.value}" );
			// }
			// else {
			// 	if( transform.Scale > 1f ) shouldBreak.Value = true;
			// }
		}
	}
}

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))] // We are updating after `PhysicsSimulationGroup` - this means that we will get the events of the current frame.
public partial struct DetectPopTargetsSys : ISystem {
	EntityQuery bubbleQuery;
	
	public void OnCreate( ref SystemState state ) {
		bubbleQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, Blowing, Simulate>() );
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate( bubbleQuery );
	}

	[BurstCompile] public void OnUpdate(ref SystemState state) {
		state.Dependency = new DetectPopTargetsJob {
			blowingLookup = GetComponentLookup<Blowing>( isReadOnly: true ),
			localTransformLookup = GetComponentLookup<LocalTransform>( isReadOnly: true ),
			poppedLookup = GetComponentLookup<Popped>(),
			networkTime = GetSingleton<NetworkTime>(),
			predictDestroyLookup = GetComponentLookup<PredictDestroy>(),
		}.Schedule( GetSingleton<SimulationSingleton>(), state.Dependency );
	}
	
	[BurstCompile] public partial struct DetectPopTargetsJob : ITriggerEventsJob {
		[ReadOnly] public ComponentLookup<Blowing> blowingLookup;
		[ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
		[ReadOnly] public ComponentLookup<PredictDestroy> predictDestroyLookup;
		public ComponentLookup<Popped> poppedLookup;
		public NetworkTime networkTime;
		
		public void Execute( TriggerEvent collisionEvent ) {
			if( !networkTime.IsFirstTimeFullyPredictingTick ) return;
			if( predictDestroyLookup.HasComponent( collisionEvent.EntityA ) && predictDestroyLookup.IsComponentEnabled( collisionEvent.EntityA ) ) return;
			if( predictDestroyLookup.HasComponent( collisionEvent.EntityB ) && predictDestroyLookup.IsComponentEnabled( collisionEvent.EntityB ) ) return;
			
			var blowingA = blowingLookup.HasComponent( collisionEvent.EntityA ) && blowingLookup.IsComponentEnabled( collisionEvent.EntityA );
			var blowingB = blowingLookup.HasComponent( collisionEvent.EntityB ) && blowingLookup.IsComponentEnabled( collisionEvent.EntityB );
			
			if( blowingA ) {
				// trigger overlap with a blowing bubble

				if( blowingB ) {
					// another blowing bubble!

					var transformA = localTransformLookup[ collisionEvent.EntityA ];
					var transformB = localTransformLookup[ collisionEvent.EntityB ];

					var entityToPop = transformA.Scale > transformB.Scale ? collisionEvent.EntityB : collisionEvent.EntityA;
					poppedLookup.SetComponentEnabled( entityToPop, true );
				}
				else {
					poppedLookup.SetComponentEnabled( collisionEvent.EntityA, true );
				}
			}
			else if( blowingB ) {
				poppedLookup.SetComponentEnabled( collisionEvent.EntityB, true );
			}
		}
	}
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct PopBubbleSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, Popped, MaterialMeshInfo, CharacterRef, Simulate>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new PopBubbleSideEffectsJob {
			blowingLookup = GetComponentLookup<Blowing>(),
			stunnedTimeLookup = GetComponentLookup<StunnedTime>(),
			stunnedLookup = GetComponentLookup<Stunned>(),
			fullLookup = GetComponentLookup<Full>(),
			ballIndexRefElementLookup = GetBufferLookup<BallIndexRefElement>(),
			effectDisplayReqElementLookup = GetBufferLookup<EffectDisplayReqElement>(),
		}.Schedule( query );
	}

	[BurstCompile] partial struct PopBubbleSideEffectsJob : IJobEntity {
		public ComponentLookup<Blowing> blowingLookup;
		public ComponentLookup<StunnedTime> stunnedTimeLookup;
		public ComponentLookup<Stunned> stunnedLookup;
		public ComponentLookup<Full> fullLookup;
		public BufferLookup<BallIndexRefElement> ballIndexRefElementLookup;
		public BufferLookup<EffectDisplayReqElement> effectDisplayReqElementLookup;
		
		void Execute( EnabledRefRW<MaterialMeshInfo> rendererEnabled, in CharacterRef characterRef ) {
			rendererEnabled.ValueRW = false;
			var blowing = blowingLookup.GetEnabledRefRW<Blowing>( characterRef.value );
			blowing.ValueRW = false;

			var stunnedTime = stunnedTimeLookup.GetRefRW( characterRef.value );
			stunnedTime.ValueRW.time = stunnedTime.ValueRO.maxTime;
			stunnedLookup.SetComponentEnabled( characterRef.value, true );
			fullLookup.SetComponentEnabled( characterRef.value, false );

			var chewingBalls = ballIndexRefElementLookup[ characterRef.value ];
			chewingBalls.Clear();

			var effectRequests = effectDisplayReqElementLookup[ characterRef.value ];
			effectRequests.Clear();
		}
	}
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PopBubbleOnServerSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Bubble, Popped>().WithPresent<Destroy>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( !GetSingleton<NetworkTime>().IsFirstTimeFullyPredictingTick ) return;

		new FlagBubbleForDestructionJob {}.Schedule( query );
	}

	[BurstCompile] partial struct FlagBubbleForDestructionJob : IJobEntity {
		void Execute( EnabledRefRW<Destroy> destroyEnabled ) {
			destroyEnabled.ValueRW = true;
		}
	}
}