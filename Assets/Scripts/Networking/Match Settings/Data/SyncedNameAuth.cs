using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class SyncedNameAuth : MonoBehaviour {
	public string defaultName;

	public class Baker : Baker<SyncedNameAuth> {

		public override void Bake( SyncedNameAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new SyncedName { value = auth.defaultName } );
		}
	}
}

[GhostComponent]
public struct SyncedName : IComponentData {
	[GhostField] public FixedString32Bytes value;
}