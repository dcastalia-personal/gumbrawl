using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class ReqPlayerNameChangeRPCAuth : MonoBehaviour {

	public class Baker : Baker<ReqPlayerNameChangeRPCAuth> {

		public override void Bake( ReqPlayerNameChangeRPCAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ReqPlayerNameChangeRPC {} );
			AddComponent( self, new SendRpcCommandRequest {} );
		}
	}
}

public struct ReqPlayerNameChangeRPC : IRpcCommand {
	public FixedString32Bytes name;
}