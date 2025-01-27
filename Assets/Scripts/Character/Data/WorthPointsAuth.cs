using Unity.Entities;
using UnityEngine;

public class WorthPointsAuth : MonoBehaviour {
	public int value;

	public class Baker : Baker<WorthPointsAuth> {

		public override void Bake( WorthPointsAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new WorthPoints { value = (short)auth.value } );
		}
	}
}

public struct WorthPoints : IComponentData {
	public short value;
}