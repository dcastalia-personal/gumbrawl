using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class DestructibleAuth : MonoBehaviour {

	public class Baker : Baker<DestructibleAuth> {

		public override void Bake( DestructibleAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Destroy {} );
			SetComponentEnabled<Destroy>( self, false );
		}
	}
}

public struct Destroy : IComponentData, IEnableableComponent {}
public struct PredictDestroy : IComponentData, IEnableableComponent {}