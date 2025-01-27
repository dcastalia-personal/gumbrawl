using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class EnterGameReqAuth : MonoBehaviour {

	public class Baker : Baker<EnterGameReqAuth> {

		public override void Bake( EnterGameReqAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new EnterGameReq {} );
			AddComponent( self, new SendRpcCommandRequest {} );
		}
	}
}

public struct EnterGameReq : IRpcCommand {}