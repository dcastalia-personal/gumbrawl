using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitCharacterSys : ISystem {

	EntityQuery query;
	EntityQuery playersQuery;
	
	[BurstCompile] public void OnCreate( ref SystemState state ) {
		playersQuery = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Player>() );
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, TeamIndexLocal, GhostOwner, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		var players = playersQuery.ToEntityArray( Allocator.Temp );

		foreach( var (owner, playerRef, teamIndex) in Query<RefRO<GhostOwner>, RefRW<PlayerRef>, RefRW<TeamIndexLocal>>().WithAll<Character>() ) {
			
			// assign a player ref to each character
			foreach( var playerEntity in players ) {
				var playerOwner = GetComponent<GhostOwner>( playerEntity );
				if( playerOwner.NetworkId != owner.ValueRO.NetworkId ) continue;

				playerRef.ValueRW.value = playerEntity;
				teamIndex.ValueRW = new TeamIndexLocal { value = GetComponent<TeamIndex>( playerEntity ).value };
				
				break;
			}
		}
	}
}