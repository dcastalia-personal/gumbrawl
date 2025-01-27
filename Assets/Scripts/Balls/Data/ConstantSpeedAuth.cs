using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class ConstantSpeedAuth : MonoBehaviour {

	public class Baker : Baker<ConstantSpeedAuth> {

		public override void Bake( ConstantSpeedAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ConstantSpeed {} );
		}
	}
}

[GhostComponent]
public struct ConstantSpeed : IComponentData {
	[GhostField] public float value;
}