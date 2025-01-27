using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Entities.SystemAPI;

// [UpdateInGroup(typeof(InputSysGroup), OrderFirst = true )] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// public partial struct ReleasePointerEventsSys : ISystem {
//
// 	[BurstCompile] public void OnCreate( ref SystemState state ) {
// 		state.RequireForUpdate<PointerActions>();
// 		state.RequireForUpdate<LevelSettings>();
// 	}
//
// 	[BurstCompile] public void OnUpdate( ref SystemState state ) {
// 		var inputSingleton = GetSingletonEntity<PointerActions>();
// 		SetComponentEnabled<EatPressed>( inputSingleton, false );
// 		SetComponentEnabled<EatReleased>( inputSingleton, false );
// 	}
// }

[UpdateInGroup(typeof(InputSysGroup))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CollectInput : ISystem {
	const string eatInput = "Eat";
	const string blowInput = "Blow";
	
	public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Controls>();
	}

	public void OnUpdate( ref SystemState state ) {
		var pointerActions = GetSingletonEntity<Controls>();
		
		SetComponentEnabled<EatPressed>( pointerActions, InputSystem.actions[eatInput].WasPressedThisFrame() );
		SetComponentEnabled<EatReleased>( pointerActions, InputSystem.actions[eatInput].WasReleasedThisFrame() );
		SetComponentEnabled<BlowPressed>( pointerActions, InputSystem.actions[blowInput].WasPressedThisFrame() );
		SetComponentEnabled<BlowReleased>( pointerActions, InputSystem.actions[blowInput].WasReleasedThisFrame() );
		SetComponent( pointerActions, new MovementInput { value = InputSystem.actions[ "Move" ].ReadValue<Vector2>() } );
	}
}

[UpdateInGroup(typeof(InputSysGroup))] [UpdateAfter(typeof(CollectInput))] [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[BurstCompile] public partial struct ProcessPointerInput : ISystem {

	EntityQuery characterQuery;
	
	public void OnCreate( ref SystemState state ) {
		state.RequireForUpdate<Controls>();
		characterQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, CharacterInput, GhostOwnerIsLocal>() );
		state.RequireForUpdate( characterQuery );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var inputSingleton = GetSingletonEntity<Controls>();

		new ApplyCharacterInputJob {
			eatPressed = IsComponentEnabled<EatPressed>( inputSingleton ),
			eatReleased = IsComponentEnabled<EatReleased>( inputSingleton ),
			blowPressed = IsComponentEnabled<BlowPressed>( inputSingleton ),
			blowReleased = IsComponentEnabled<BlowReleased>( inputSingleton ),
			movement = GetComponent<MovementInput>( inputSingleton ).value,
		}.ScheduleParallel( characterQuery );
	}

	[BurstCompile] partial struct ApplyCharacterInputJob : IJobEntity {
		public bool eatPressed;
		public bool eatReleased;
		public bool blowPressed;
		public bool blowReleased;
		public float2 movement;
		
		void Execute( ref CharacterInput characterInput, in Character character ) {
			var oldMovement = characterInput.movementDelta;
			characterInput = default;
			if( eatPressed ) characterInput.eatPress.Set();
			if( eatReleased ) characterInput.eatRelease.Set();
			if( blowPressed ) characterInput.blowPress.Set();
			if( blowReleased ) characterInput.blowRelease.Set();

			characterInput.movementDelta = Vector2.MoveTowards( oldMovement, movement, character.acceleration );
		}
	}
}