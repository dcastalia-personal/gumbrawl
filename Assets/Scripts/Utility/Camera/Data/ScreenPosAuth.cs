using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ScreenPosAuth : MonoBehaviour {

	public class Baker : Baker<ScreenPosAuth> {

		public override void Bake( ScreenPosAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new ScreenPos {} );
		}
	}
}

public struct ScreenPos : IComponentData {
	public float2 value;
}