using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class ReqPlayerTeamChangeRPCAuth : MonoBehaviour {

	public class Baker : Baker<ReqPlayerTeamChangeRPCAuth> {

		public override void Bake( ReqPlayerTeamChangeRPCAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ReqPlayerTeamChangeRPC {} );
			AddComponent( self, new SendRpcCommandRequest {} );
		}
	}
}

public struct ReqPlayerTeamChangeRPC : IRpcCommand {
	public short team;
}