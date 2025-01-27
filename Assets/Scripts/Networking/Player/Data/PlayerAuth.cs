using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class PlayerAuth : MonoBehaviour {
	public class Baker : Baker<PlayerAuth> {

		public override void Bake( PlayerAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Player {} );
			AddComponent( self, new TeamIndex {} );
			AddComponent( self, new PlayerUIRef {} );
			AddComponent( self, new Score {} );
		}
	}
}

public struct Player : IComponentData {
	public Entity connection;
}

// put on the network connection on the server to make mapping them easier
public struct PlayerRef : IComponentData {
	public Entity value;
}

public struct PlayerUIRef : IComponentData {
	public UnityObjectRef<PlayerScoreEntryUI> value;
}