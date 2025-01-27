using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class CopyDataFromTargetAuth : MonoBehaviour {

	public class Baker : Baker<CopyDataFromTargetAuth> {

		public override void Bake( CopyDataFromTargetAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new CopyDataFromTarget {} );
			SetComponentEnabled<CopyDataFromTarget>( self, false );
		}
	}
}

[GhostComponent]
public struct CopyDataFromTarget : IComponentData, IEnableableComponent {
	[GhostField] public Entity value;
}