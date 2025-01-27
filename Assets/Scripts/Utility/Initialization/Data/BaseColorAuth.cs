using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class BaseColorAuth : MonoBehaviour {
	public Color initColor;

	public class Baker : Baker<BaseColorAuth> {

		public override void Bake( BaseColorAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			var rend = auth.GetComponent<MeshRenderer>();
			AddComponent( self, new BaseColor { value = !rend || auth.initColor != Color.clear ? (Vector4)auth.initColor : (Vector4)auth.GetComponent<MeshRenderer>().sharedMaterial.color } );
		}
	}
}

[MaterialProperty( "_BaseColor" )]
public struct BaseColor : IComponentData {
	public float4 value;
}