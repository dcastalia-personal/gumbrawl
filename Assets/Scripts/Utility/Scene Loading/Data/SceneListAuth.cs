using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public class SceneListAuth : MonoBehaviour {
	
#if UNITY_EDITOR
	
	public List<UnityEditor.SceneAsset> scenes = new();

	public class Baker : Baker<SceneListAuth> {

		public override void Bake( SceneListAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new SceneList {} );
			
			var buf = AddBuffer<SceneEntry>( self );

			foreach( var sceneAsset in auth.scenes ) {
				buf.Add( new SceneEntry { sceneRef = new EntitySceneReference( sceneAsset ) } );
			}

			AddComponent( self, new LoadScene {} );
			AddComponent( self, new LoadSceneEnabled {} ); SetComponentEnabled<LoadSceneEnabled>( self, false );
			
			AddComponent( self, new UnloadScene {} );
			AddComponent( self, new UnloadSceneEnabled {} ); SetComponentEnabled<UnloadSceneEnabled>( self, false );
		}
	}
	
#endif
}

public struct SceneList : IComponentData {
	
}

public struct SceneEntry : IBufferElementData {
	public EntitySceneReference sceneRef;
	public Entity loadedSceneHandle;
}

public struct LoadScene : IComponentData {
	public int sceneIndex;
}

public struct UnloadScene : IComponentData {
	public int sceneIndex;
}

public struct LoadSceneEnabled : IComponentData, IEnableableComponent {}
public struct UnloadSceneEnabled : IComponentData, IEnableableComponent {}