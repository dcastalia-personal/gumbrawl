using Unity.Entities;
using UnityEngine;

public class StartingLocationAuth : MonoBehaviour {

	public class Baker : Baker<StartingLocationAuth> {

		public override void Bake( StartingLocationAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new StartingLocation {} );
		}
	}
}

public struct StartingLocation : IComponentData {
	
}