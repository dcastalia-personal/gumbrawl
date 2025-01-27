using Unity.Entities;
using UnityEngine;

public class CountTowardsLevelCompletionAuth : MonoBehaviour {

	public class Baker : Baker<CountTowardsLevelCompletionAuth> {

		public override void Bake( CountTowardsLevelCompletionAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new CountTowardsLevelCompletion {} );
		}
	}
}

public struct CountTowardsLevelCompletion : IComponentData {
	
}