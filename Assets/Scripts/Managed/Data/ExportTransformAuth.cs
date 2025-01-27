using Unity.Entities;
using UnityEngine;

public class ExportTransformAuth : MonoBehaviour {

	public class Baker : Baker<ExportTransformAuth> {

		public override void Bake( ExportTransformAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new ExportTransform {} );
		}
	}
}

public struct ExportTransform : IComponentData {
	public UnityObjectRef<GameObject> target;
}