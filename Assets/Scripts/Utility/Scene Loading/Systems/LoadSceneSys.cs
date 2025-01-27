using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateBefore(typeof(LoadSceneSys))]
public partial struct CleanupManagedObjectBetweenScenesSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadScene, LoadSceneEnabled>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var managedObjects = Object.FindObjectsByType<CleanUpBetweenScenes>( FindObjectsInactive.Include, FindObjectsSortMode.None );
		foreach( var managedObject in managedObjects ) Object.Destroy( managedObject.gameObject );

		// Debug.Log( $"Cleaned up {managedObjects.Length} managed objects" );
	}
}

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateBefore(typeof(SceneSystemGroup))]
public partial struct LoadSceneSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LoadScene, LoadSceneEnabled>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var loadSceneEntity = GetSingletonEntity<LoadScene>();
		var loadSceneReq = GetComponent<LoadScene>( loadSceneEntity );
		var sceneList = GetSingletonBuffer<SceneEntry>();
		SetComponentEnabled<LoadSceneEnabled>( loadSceneEntity, false );

		// Debug.Log( $"Load scene system loading scene {loadSceneReq.sceneIndex} on client? {state.WorldUnmanaged.IsClient()}" );
		var sceneEntryToLoad = sceneList[ loadSceneReq.sceneIndex ];
		
		// if the scene is already loaded, unload it first
		if( sceneEntryToLoad.loadedSceneHandle != Entity.Null ) {
			var loadedSceneHandle = sceneEntryToLoad.loadedSceneHandle;
			sceneEntryToLoad.loadedSceneHandle = Entity.Null;
			SceneSystem.UnloadScene( state.WorldUnmanaged, loadedSceneHandle );
			sceneList = GetSingletonBuffer<SceneEntry>();
		}
		
		sceneEntryToLoad.loadedSceneHandle = SceneSystem.LoadSceneAsync( state.WorldUnmanaged, sceneList[ loadSceneReq.sceneIndex ].sceneRef, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
		sceneList = GetSingletonBuffer<SceneEntry>();
		sceneList[ loadSceneReq.sceneIndex ] = sceneEntryToLoad;
	}
}

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateBefore(typeof(LoadSceneSys))]
public partial struct UnloadSceneSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<UnloadScene, UnloadSceneEnabled>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		if( query.IsEmpty ) return;

		var unloadSceneEntity = GetSingletonEntity<UnloadScene>();
		var unloadSceneReq = GetComponent<UnloadScene>( unloadSceneEntity );
		var sceneList = GetSingletonBuffer<SceneEntry>();
		SetComponentEnabled<UnloadSceneEnabled>( unloadSceneEntity, false );
		
		// Debug.Log( $"Unload scene system unloading scene {unloadSceneReq.sceneIndex} on client? {state.WorldUnmanaged.IsClient()}" );
		SceneSystem.UnloadScene( state.WorldUnmanaged, sceneList[ unloadSceneReq.sceneIndex ].loadedSceneHandle );
	}
}