using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class BallAuth : MonoBehaviour {
	public BallsListAuth list;
	public GameObject proxy;

	public class Baker : Baker<BallAuth> {

		public override void Bake( BallAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Ball { prefabIndex = (short)auth.list.gumballPrefabs.FindIndex( ball => ball == auth.gameObject ) } );
			AddComponent<PredictDestroy>( self ); SetComponentEnabled<PredictDestroy>( self, false );

			if( auth.proxy ) {
				AddComponent( self, new ProxyRef {} );
				AddComponent( self, new PursueTarget {} ); SetComponentEnabled<PursueTarget>( self, false );
			}

			AddComponent<ServerIndex>( self );
		}
	}
}

public struct Ball : IComponentData {
	public short prefabIndex;
}

public struct ProxyRef : IComponentData, IEnableableComponent {
	public Entity target;
}

public struct ReqBallProxyRPC : IRpcCommand {
	public short sourceIndex;
	public int serverIndex;
	public float2 position;
	public Entity target;
}