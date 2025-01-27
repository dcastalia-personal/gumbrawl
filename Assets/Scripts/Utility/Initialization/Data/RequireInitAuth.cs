using Unity.Entities;
using UnityEngine;

public class RequireInitAuth : MonoBehaviour {

	public class Baker : Baker<RequireInitAuth> {

		public override void Bake( RequireInitAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new RequireInit {} );
		}
	}
}

public struct RequireInit : IComponentData {}