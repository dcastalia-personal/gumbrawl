using Unity.Entities;
using UnityEngine;

public class NextBubbleHeavyEffectAuth : MonoBehaviour {
	public float modifyGravityScale;

	public class Baker : Baker<NextBubbleHeavyEffectAuth> {

		public override void Bake( NextBubbleHeavyEffectAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new NextBubbleHeavyEffect { modifyGravityScale = auth.modifyGravityScale } );
		}
	}
}

public struct NextBubbleHeavyEffect : IComponentData {
	public const float min = -2f;
	public const float max = 2f;
	
	public float modifyGravityScale;
}