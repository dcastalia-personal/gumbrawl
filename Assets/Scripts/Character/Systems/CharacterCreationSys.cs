using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(InitAfterSceneSysGroup))][WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct CharacterCreationSys : ISystem {
	EntityQuery inGameQuery;
	EntityQuery startingLocationsQuery;
	
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		inGameQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<LevelSettings, RequireInit>() );
		state.RequireForUpdate<Player>();
		state.RequireForUpdate( inGameQuery );
		
		startingLocationsQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<StartingLocation>() );
		state.RequireForUpdate( startingLocationsQuery );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var ecbSingleton = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var ecb = ecbSingleton.CreateCommandBuffer( state.WorldUnmanaged );

		var startingLocations = startingLocationsQuery.ToEntityArray( Allocator.Temp );

		int startingLocationIndex = 0;
		foreach( var (owner, self) in Query<RefRO<GhostOwner>>().WithAll<Player>().WithEntityAccess() ) {
			var levelSettings = GetSingleton<LevelSettings>();
			var characterEntity = ecb.Instantiate( levelSettings.characterPrefab );
			ecb.SetComponent( characterEntity, owner.ValueRO );

			ecb.SetComponent( characterEntity, GetComponent<LocalTransform>( startingLocations[ startingLocationIndex ] ) );
			startingLocationIndex = (startingLocationIndex + 1) % startingLocations.Length;

			ecb.AppendToBuffer( self, new LinkedEntityGroup { Value = characterEntity } );
			ecb.SetComponent( characterEntity, new SkinColor { value = (Vector3)(Vector4)Random.ColorHSV( 0f, 1f, 0.2f, 0.2f, 1f, 1f ) } );
		}
	}
}