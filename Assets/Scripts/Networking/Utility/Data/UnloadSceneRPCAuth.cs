using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class UnloadSceneRPCAuth : MonoBehaviour {

	public class Baker : Baker<UnloadSceneRPCAuth> {

		public override void Bake( UnloadSceneRPCAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new UnloadSceneRPC {} );
			AddComponent( self, new SendRpcCommandRequest() );
		}
	}
}

public struct UnloadSceneRPC : IRpcCommand {
	public short sceneIndex;
}