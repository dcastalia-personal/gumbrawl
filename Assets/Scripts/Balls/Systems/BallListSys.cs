using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )] [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InitBallsListOnServerSys : ISystem {

	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<BallsList, PossibleColorElement, Randomness, RequireInit>() );
		state.RequireForUpdate( query );
	}

	[BurstCompile] public void OnUpdate( ref SystemState state ) {
		new InitBallsListOnServerJob {
			ballsListEntryLookup = GetBufferLookup<BallColor>(),
		}.Schedule( query );
	}

	[BurstCompile] partial struct InitBallsListOnServerJob : IJobEntity {
		public BufferLookup<BallColor> ballsListEntryLookup;
		
		void Execute( Entity self, DynamicBuffer<PossibleColorElement> possibleColors, ref Randomness randomness ) {
			// Debug.Log( $"Initializing colors on balls list" );
			var colorCandidates = new NativeList<PossibleColorElement>( Allocator.Temp );
			var colorPossibilities = possibleColors.ToNativeArray( Allocator.Temp );
			colorCandidates.CopyFrom( colorPossibilities );

			var ballEntries = ballsListEntryLookup[ self ];

			for( int index = 0; index < ballEntries.Length; index++ ) {
				var candidateIndex = randomness.rng.NextInt( 0, colorCandidates.Length );
				var ballListEntry = ballEntries[ index ];
				ballListEntry.value = colorCandidates[ candidateIndex ].color;
				ballEntries[ index ] = ballListEntry;

				colorCandidates.RemoveAtSwapBack( candidateIndex );

				if( colorCandidates.Length == 0 ) {
					colorCandidates.CopyFrom( colorPossibilities );
				}
			}
			
		}
	}
}