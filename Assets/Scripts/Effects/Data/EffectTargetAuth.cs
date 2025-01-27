using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class EffectTargetAuth : MonoBehaviour {

	public class Baker : Baker<EffectTargetAuth> {

		public override void Bake( EffectTargetAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new EffectTarget {} );
		}
	}
}

[GhostComponent]
public struct EffectTarget : IComponentData {
	[GhostField] public Entity value;
}

[GhostComponent]
public struct EffectDisplayReqElement : IBufferElementData {
	// public UnityObjectRef<GameObject> prefab;
	[GhostField] public short ballPrefabIndex;
}