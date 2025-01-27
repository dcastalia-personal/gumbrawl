using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;

// [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
// public partial struct ManualBounceSys : ISystem {
// 	EntityQuery ballQuery;
// 	
// 	public void OnCreate( ref SystemState state ) {
// 		ballQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Ball, PhysicsVelocity, PhysicsCollider, LocalTransform>() );
// 		state.RequireForUpdate( ballQuery );
// 		
// 		state.RequireForUpdate<PhysicsWorldSingleton>();
// 	}
//
// 	[BurstCompile] public void OnUpdate(ref SystemState state) {
// 		new ManualBounceJob {
// 			collisionWorld = GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
// 			fixedTimeStep = SystemAPI.Time.fixedDeltaTime,
// 		}.ScheduleParallel( ballQuery );
// 	}
//
// 	[BurstCompile] unsafe partial struct ManualBounceJob : IJobEntity {
// 		[ReadOnly] public CollisionWorld collisionWorld;
// 		public float fixedTimeStep;
// 		
// 		void Execute( Entity self, ref PhysicsVelocity velocity, in PhysicsCollider collider, in LocalTransform transform ) {
// 			var castInput = new ColliderCastInput {
// 				Collider = collider.ColliderPtr,
// 				Start = transform.Position,
// 				End = transform.Position + velocity.Linear * fixedTimeStep,
// 			};
//
// 			var hits = new NativeList<ColliderCastHit>( Allocator.Temp );
//
// 			if( collisionWorld.CastCollider( castInput, ref hits ) ) {
//
// 				foreach( var hit in hits ) {
// 					if( hit.Entity == self ) continue;
// 					
// 					var reflection = math.reflect( velocity.Linear, hit.SurfaceNormal );
// 					reflection.z = 0f;
// 					
// 					velocity.Linear = reflection;
// 					break;
// 				}
// 				
// 			}
// 		}
// 	}
// }