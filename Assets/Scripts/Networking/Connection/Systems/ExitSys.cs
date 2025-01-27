using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static Unity.Entities.SystemAPI;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DisconnectSys : ISystem {
	EntityQuery query;

	public void OnUpdate( ref SystemState state ) {
		if( Keyboard.current.escapeKey.wasPressedThisFrame ) {
			state.EntityManager.AddComponent<NetworkStreamRequestDisconnect>( QueryBuilder().WithAll<NetworkId>().Build() );

			var matchSettingsEntity = GetSingletonEntity<MatchSettings>();
			SetComponentEnabled<WaitForDisconnect>( matchSettingsEntity, true );
		}
	}
}

public partial struct ExitSys : ISystem {
	EntityQuery noConnectionQuery;
	EntityQuery lookingForDisconnectQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		noConnectionQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithNone<NetworkId>() );
		lookingForDisconnectQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<WaitForDisconnect>() );
		state.RequireForUpdate( noConnectionQuery );
		state.RequireForUpdate( lookingForDisconnectQuery );
	}

	public void OnUpdate( ref SystemState state ) {
		if( lookingForDisconnectQuery.IsEmpty ) return;
		if( noConnectionQuery.IsEmpty ) return;

		var defaultWorld = DefaultWorldInitialization.Initialize( "DefaultWorld" );
		World.DefaultGameObjectInjectionWorld = defaultWorld;

		SceneManager.LoadScene( "Main" );
	}
}