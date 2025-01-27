using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PursueTargetAuth : MonoBehaviour {
	public float speed;
	public float time;

	public class Baker : Baker<PursueTargetAuth> {

		public override void Bake( PursueTargetAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new PursueTarget {} );
			AddComponent( self, new Speed { value = auth.speed } );
			AddComponent( self, new Duration { value = auth.time } );
		}
	}
}

public struct PursueTarget : IComponentData, IEnableableComponent {
	public Entity value;
}

public struct Duration : IComponentData {
	public float value;
}