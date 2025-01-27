using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class BallsListAuth : MonoBehaviour {
	public List<GameObject> gumballPrefabs;
	public List<Color> possibleColors;
	public GameObject ballProxyPrefab;
	public GameObject effectUIPrefab;

	public class Baker : Baker<BallsListAuth> {

		public override void Bake( BallsListAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new BallsList {
				ballProxyPrefab = GetEntity( auth.ballProxyPrefab, TransformUsageFlags.Dynamic ),
			} );

			var balls = AddBuffer<BallColor>( self );
			
			var prefabs = AddBuffer<BallPrefabRef>( self );
			foreach( var gumballPrefab in auth.gumballPrefabs ) {
				prefabs.Add( new BallPrefabRef {
					value = GetEntity( gumballPrefab, TransformUsageFlags.Dynamic )
				} );
			}
			
			balls.Resize( prefabs.Length, NativeArrayOptions.UninitializedMemory );

			var colors = AddBuffer<PossibleColorElement>( self );

			foreach( var color in auth.possibleColors ) {
				colors.Add( new PossibleColorElement { color = (Vector4)color } );
			}

			AddComponent( self, new EffectUIPrefabRef { value = new UnityObjectRef<GameObject> { Value = auth.effectUIPrefab } } );
		}
	}
}

public struct BallsList : IComponentData {
	public Entity ballProxyPrefab;
}

[GhostComponent]
public struct BallColor : IBufferElementData {
	[GhostField] public float4 value;
}

public struct BallPrefabRef : IBufferElementData {
	public Entity value;
}

public struct PossibleColorElement : IBufferElementData {
	public float4 color;
}

[InternalBufferCapacity( 3 )] // it feels weird this not being a ghost component, but when it is, the value is sometimes rolled back to empty even though the ball's been eaten
public struct BallIndexRefElement : IBufferElementData {
	public short value;
}

public struct EffectUIPrefabRef : IComponentData {
	public UnityObjectRef<GameObject> value;
}