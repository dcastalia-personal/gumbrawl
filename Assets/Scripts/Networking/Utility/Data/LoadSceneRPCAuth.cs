using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class LoadSceneRPCAuth : MonoBehaviour {

	public SceneListAuth sceneList;
	
#if UNITY_EDITOR
	
	public UnityEditor.SceneAsset sceneToLoad;

	public class Baker : Baker<LoadSceneRPCAuth> {

		public override void Bake( LoadSceneRPCAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );

			for( int index = 0; index < auth.sceneList.scenes.Count; index++ ) {
				if( string.Equals( auth.sceneList.scenes[ index ].name, auth.sceneToLoad.name, StringComparison.Ordinal ) ) {
					AddComponent( self, new LoadSceneRPC { sceneIndex = (short)auth.sceneList.scenes.IndexOf( auth.sceneToLoad ) } );
					break;
				}
			}
			
			AddComponent( self, new SendRpcCommandRequest() );
		}
	}
	
#endif
	
}

public struct LoadSceneRPC : IRpcCommand {
	public short sceneIndex;
}