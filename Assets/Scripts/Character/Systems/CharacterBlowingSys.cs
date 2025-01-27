using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CharacterStartBlowingSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp )
			.WithAll<Blow, GhostOwner, CharacterInput, Chewing, BallIndexRefElement, PlayerRef, Simulate>()
			.WithDisabled<Blowing>()
			.WithAll<LocalTransform>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new CreateBubbleJob {
			ecb = ecb,
			localToWorldLookup = GetComponentLookup<LocalToWorld>( isReadOnly: true ),
			networkTime = GetSingleton<NetworkTime>(),
		}.Schedule( query );
	}

	[BurstCompile] partial struct CreateBubbleJob : IJobEntity {
		public EntityCommandBuffer ecb;
		[ReadOnly] public ComponentLookup<LocalToWorld> localToWorldLookup;
		public NetworkTime networkTime;
		
		void Execute( Entity self, in Blow blow, in CharacterInput input, EnabledRefRW<Chewing> chewingEnabled, EnabledRefRW<Blowing> blowingEnabled, 
			in PlayerRef playerRef, in GhostOwner owner, in LocalTransform transform ) {
			if( input.blowPress.IsSet ) {
				blowingEnabled.ValueRW = true;
				chewingEnabled.ValueRW = false;

				if( !networkTime.IsFirstTimeFullyPredictingTick ) return;

				var bubble = ecb.Instantiate( blow.blowingBubblePrefab );
				
				ecb.SetComponent( bubble, new CharacterRef { value = self } );
				ecb.SetComponent( bubble, new SpawnTick { value = networkTime.ServerTick } );
				ecb.SetComponent( bubble, owner );
				
				var newBlower = blow;
				newBlower.bubble = bubble;
				ecb.SetComponent( self, newBlower );

				ecb.SetComponent( bubble, new LocalTransform { Position = transform.Position + blow.bubbleSpawnOffset, Rotation = quaternion.identity, Scale = 1f } );
				ecb.SetComponentEnabled<Blowing>( bubble, true );
			}
		}
	}
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CharacterStopBlowingSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp )
			.WithAll<Character, Blow, GhostOwner, CharacterInput, Blowing, BallIndexRefElement, Simulate>()
			.WithAll<BubbleEffectRefElement, EffectDisplayReqElement>()
			.WithPresent<Chewing, Full>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new StopBlowingJob {
			ecb = ecb,
			networkTime = GetSingleton<NetworkTime>(),
			localTransformLookup = GetComponentLookup<LocalTransform>( isReadOnly: true ),
			characterRefLookup = GetComponentLookup<CharacterRef>( isReadOnly: true ),
			baseColorLookup = GetComponentLookup<BaseColor>( isReadOnly: true ),
			teamIndexLocalLookup = GetComponentLookup<TeamIndexLocal>( isReadOnly: true ),
			predictDestroyLookup = GetComponentLookup<PredictDestroy>(),
		}.Schedule( query );
	}
	
	[BurstCompile] partial struct StopBlowingJob : IJobEntity {
		public EntityCommandBuffer ecb;
		[ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
		[ReadOnly] public ComponentLookup<CharacterRef> characterRefLookup;
		[ReadOnly] public ComponentLookup<BaseColor> baseColorLookup;
		[ReadOnly] public ComponentLookup<TeamIndexLocal> teamIndexLocalLookup;
		public ComponentLookup<PredictDestroy> predictDestroyLookup;
		public NetworkTime networkTime;
		const float minBlowSize = 2f;
		
		void Execute( in Blow blower, DynamicBuffer<BallIndexRefElement> chewingBalls, in CharacterInput input, EnabledRefRW<Blowing> blowingEnabled, EnabledRefRW<Chewing> chewingEnabled,
			DynamicBuffer<BubbleEffectRefElement> bubbleEffects, DynamicBuffer<EffectDisplayReqElement> displayReqs, in GhostOwner owner, EnabledRefRW<Full> fullEnabled ) {
			if( input.blowRelease.IsSet ) {
				blowingEnabled.ValueRW = false;
				
				if( blower.bubble == Entity.Null ) return; // can happen if you click quickly and prediction has created a bubble but it gets rolled back

				var blownTransform = localTransformLookup[ blower.bubble ];
				
				if( blownTransform.Scale < minBlowSize ) {
					predictDestroyLookup.SetComponentEnabled( blower.bubble, true );
					chewingEnabled.ValueRW = true;
					return;
				}
				
				if( !networkTime.IsFirstTimeFullyPredictingTick ) return;
				
				
				var finishedBubble = ecb.Instantiate( blower.finishedBubblePrefab );

				ecb.SetComponentEnabled<PredictDestroy>( blower.bubble, true );
				ecb.SetComponent( finishedBubble, blownTransform );
				ecb.SetComponent( finishedBubble, characterRefLookup[ blower.bubble ] );
				ecb.SetComponent( finishedBubble, baseColorLookup[ blower.bubble ] );
				ecb.SetComponent( finishedBubble, teamIndexLocalLookup[ blower.bubble ] );
				ecb.SetComponent( finishedBubble, owner );

				foreach( var effect in bubbleEffects ) {
					var effectInstance = ecb.Instantiate( effect.value );
					ecb.SetComponent( effectInstance, new EffectTarget { value = finishedBubble } );
				}

				displayReqs.Clear();
				chewingBalls.Clear();
				bubbleEffects.Clear();
				fullEnabled.ValueRW = false;
				
				// TODO: clear the UI that has bubble notifications or trigger some animation that gets them off screen and destroying themselves
			}
		}
	}
}