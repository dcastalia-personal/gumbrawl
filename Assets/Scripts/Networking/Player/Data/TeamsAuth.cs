using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class TeamsAuth : MonoBehaviour {
	public List<TeamEntryAuth> teams = new();

	public class Baker : Baker<TeamsAuth> {

		public override void Bake( TeamsAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Teams {} );
			var teams = AddBuffer<Team>( self );
			
			foreach( var teamAuth in auth.teams ) {
				teams.Add( new Team { color = (Vector4)teamAuth.color } );
			}

			var scores = AddBuffer<ScoreElement>( self );
			foreach( var teamAuth in auth.teams ) {
				scores.Add( new ScoreElement {} );
			}
		}
	}
}

public struct Teams : IComponentData {
	
}

[InternalBufferCapacity( 0 )]
public struct Team : IBufferElementData {
	public float4 color;
}

[Serializable] public class TeamEntryAuth {
	public Color color;
}

[GhostComponent]
public struct TeamIndex : IComponentData {
	[GhostField] public short value;
}

public struct TeamIndexLocal : IComponentData {
	public short value;
}

// TODO: Set team color on init