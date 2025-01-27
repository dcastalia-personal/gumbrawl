using Unity.Entities;
using UnityEngine;

public class ModifySpeedEffectAuth : MonoBehaviour {
	public float amount;

	public class Baker : Baker<ModifySpeedEffectAuth> {

		public override void Bake( ModifySpeedEffectAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new ModifySpeedEffect { amount = auth.amount } );
		}
	}
}

public struct ModifySpeedEffect : IComponentData {
	public const float min = 1f;
	public const float max = 10f;
	
	public float amount;
}