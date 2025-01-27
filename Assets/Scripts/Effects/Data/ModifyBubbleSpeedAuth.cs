using Unity.Entities;
using UnityEngine;

public class ModifyBubbleSpeedAuth : MonoBehaviour {
	public float amount;

	public class Baker : Baker<ModifyBubbleSpeedAuth> {

		public override void Bake( ModifyBubbleSpeedAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new ModifyBubbleSpeed { amount = auth.amount } );
		}
	}
}

public struct ModifyBubbleSpeed : IComponentData {
	public const float min = 1f;
	public const float max = 3f;
	
	public float amount;
}