using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class SceneFinishedLoadingRPCAuth : MonoBehaviour {

	public class Baker : Baker<SceneFinishedLoadingRPCAuth> {

		public override void Bake( SceneFinishedLoadingRPCAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new SceneFinishedLoadingRPC {} );
			AddComponent( self, new SendRpcCommandRequest {} );
		}
	}
}

public struct SceneFinishedLoadingRPC : IRpcCommand {}