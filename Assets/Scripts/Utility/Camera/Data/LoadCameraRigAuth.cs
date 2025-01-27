using Unity.Entities;
using UnityEngine;

public class LoadCameraRigAuth : MonoBehaviour {
	public GameObject cameraRigPrefab;

	public class Baker : Baker<LoadCameraRigAuth> {

		public override void Bake( LoadCameraRigAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LoadCameraRig { cameraRigPrefab = new UnityObjectRef<GameObject> { Value = auth.cameraRigPrefab } } );
		}
	}
}

public struct LoadCameraRig : IComponentData {
	public UnityObjectRef<GameObject> cameraRigPrefab;
}