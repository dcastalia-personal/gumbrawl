namespace Networking.Connection {

using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public class ConnectorAuth : MonoBehaviour {
	#if UNITY_EDITOR
	public GameObject uiPrefab;

	public UnityEditor.SceneAsset onlineScene;

	public class Baker : Baker<ConnectorAuth> {

		public override void Bake( ConnectorAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new Connector { 
				uiObjPrefabRef = new UnityObjectRef<GameObject> { Value = auth.uiPrefab }, 
				onlineScene = new EntitySceneReference( auth.onlineScene ),
			} );
		}
	}
	#endif
}

public struct Connector : IComponentData {
	public UnityObjectRef<GameObject> uiObjPrefabRef;
	public UnityObjectRef<ConnectorUI> uiObjInstance;
	public EntitySceneReference onlineScene;
}

}