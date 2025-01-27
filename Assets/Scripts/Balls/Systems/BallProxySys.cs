using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(CleanupGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ReplaceBallWithProxySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Ball, ServerIndex, BaseColor, ProxyRef, LocalTransform, PredictDestroy>().WithPresent<PursueTarget>() );
		query.AddChangedVersionFilter( ComponentType.ReadOnly<PredictDestroy>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;
		
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		new ReplaceBallWithProxyJob {
		    ecb = ecb,
		    proxyBall = GetSingleton<BallsList>().ballProxyPrefab,
		    recentBallProxiesCreatedLookup = GetBufferLookup<RecentBallProxiesCreated>(),
		    networkTime = GetSingleton<NetworkTime>(),
		    proxyRefLookup = GetComponentLookup<ProxyRef>(),
		}.Schedule( query );
	}

	[BurstCompile] partial struct ReplaceBallWithProxyJob : IJobEntity {
		public EntityCommandBuffer ecb;
		public Entity proxyBall;
		public BufferLookup<RecentBallProxiesCreated> recentBallProxiesCreatedLookup;
		public NetworkTime networkTime;
		public ComponentLookup<ProxyRef> proxyRefLookup;
	    
		void Execute( Entity self, in PursueTarget pursueTarget, in BaseColor baseColor, in LocalTransform transform, in ServerIndex serverIndex ) {
			
			var newProxy = ecb.Instantiate( proxyBall );
			ecb.SetComponent( newProxy, pursueTarget );
			ecb.SetComponent( newProxy, baseColor );
			ecb.SetComponent( newProxy, transform );

			var proxyRef = proxyRefLookup[ self ];
			proxyRefLookup.SetComponentEnabled( self, false );

			if( proxyRef.target == Entity.Null ) return;
			var proxyList = recentBallProxiesCreatedLookup[ proxyRef.target ];
			proxyList.Add( new RecentBallProxiesCreated { serverIndex = serverIndex.value, tick = networkTime.ServerTick } );
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PursueTargetSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<PursueTarget, Speed, Duration>().WithPresent<Destroy>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new PursueTargetJob {
		    localTransformLookup = GetComponentLookup<LocalTransform>(),
		    deltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel( query );
	}

	[BurstCompile] partial struct PursueTargetJob : IJobEntity {
		[NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> localTransformLookup;
		public float deltaTime;
		const float terminalScale = 0f;
	    
		void Execute( Entity self, in PursueTarget pursueTarget, in Speed speed, ref Duration duration, EnabledRefRW<Destroy> destroyEnabled ) {
			var transform = localTransformLookup[ self ];
			
			var targetPos = localTransformLookup[ pursueTarget.value ].Position;
			var time = speed.value * deltaTime;
			transform.Position = math.lerp( transform.Position, targetPos, time );
			transform.Scale = math.lerp( transform.Scale, terminalScale, time );
			localTransformLookup[ self ] = transform;

			if( duration.value > 0f ) {
				duration.value -= deltaTime;
				return;
			}

			destroyEnabled.ValueRW = true;
		}
	}
}

[UpdateInGroup(typeof(CleanupGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerReqBallProxySys : ISystem {
	EntityQuery query;
	EntityArchetype reqCreateBallProxyArch;

	public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Ball, ProxyRef, Destroy, LocalTransform>() );
		state.RequireForUpdate( query );
		
		var rpcTypes = new NativeArray<ComponentType>( 2, Allocator.Temp ) {};
		rpcTypes[ 0 ] = new ComponentType( typeof(ReqBallProxyRPC) );
		rpcTypes[ 1 ] = new ComponentType( typeof(SendRpcCommandRequest), ComponentType.AccessMode.ReadOnly );
		
		reqCreateBallProxyArch = state.EntityManager.CreateArchetype( rpcTypes );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		foreach( var (ball, serverIndex, transform, readyToCreateProxy, self) in Query<RefRO<Ball>, RefRO<ServerIndex>, RefRO<LocalTransform>, RefRO<ProxyRef>>()
			        .WithAll<Destroy, ProxyRef>().WithEntityAccess() ) {
			
			var rpcEntity = ecb.CreateEntity( reqCreateBallProxyArch );
			ecb.SetComponent( rpcEntity, new ReqBallProxyRPC {
				serverIndex = serverIndex.ValueRO.value,
				sourceIndex = ball.ValueRO.prefabIndex, 
				position = transform.ValueRO.Position.xy, 
				target = readyToCreateProxy.ValueRO.target
			} );
			// Debug.Log( $"Server sending rpc to create proxy at pos {transform.ValueRO.Position.xy} for target {readyToCreateProxy.ValueRO.target} with server index {serverIndex.ValueRO.value}" );
		}
	}
}

[WorldSystemFilter( WorldSystemFilterFlags.ClientSimulation )]
[UpdateInGroup( typeof(GhostSimulationSystemGroup) )]
public partial struct ReceiveCreateVisualProxySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<ReqBallProxyRPC, ReceiveRpcCommandRequest>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );
		ecb.DestroyEntity( query, EntityQueryCaptureMode.AtPlayback );

		var ballList = GetSingletonBuffer<BallColor>();
		var proxy = GetSingleton<BallsList>().ballProxyPrefab;
		foreach( var rpcCmd in Query<RefRO<ReqBallProxyRPC>>().WithAll<ReceiveRpcCommandRequest>() ) {
			
			// Debug.Log( $"Client catching rpc to create proxy for ball with server index {rpcCmd.ValueRO.serverIndex}" );
			
			// already predicted that the ball needs a proxy
			var recentProxies = GetBuffer<RecentBallProxiesCreated>( rpcCmd.ValueRO.target );
			bool proxyAlreadyPredicted = false;
			foreach( var recentProxy in recentProxies ) {
				if( recentProxy.serverIndex == rpcCmd.ValueRO.serverIndex ) {
					proxyAlreadyPredicted = true;
					break;
				}
			}

			if( proxyAlreadyPredicted ) continue;

			var origColor = ballList[ rpcCmd.ValueRO.sourceIndex ].value;
			
			var newProxy = ecb.Instantiate( proxy );
			ecb.SetComponent( newProxy, new PursueTarget { value = rpcCmd.ValueRO.target } );
			ecb.SetComponent( newProxy, new BaseColor { value = origColor } );

			var rpcPos = rpcCmd.ValueRO.position;
			ecb.SetComponent( newProxy, new LocalTransform { Position = new float3( rpcPos.x, rpcPos.y, 0f ), Rotation = quaternion.identity, Scale = 1f } );
		}
	}
}