using Unity.Entities;
using UnityEngine;

public class NextBubbleWorthEffectAuth : MonoBehaviour {
	public int modification;

	public class Baker : Baker<NextBubbleWorthEffectAuth> {

		public override void Bake( NextBubbleWorthEffectAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new NextBubbleWorthEffect { modification = (short)auth.modification } );
		}
	}
}

public struct NextBubbleWorthEffect : IComponentData {
	public const short min = 0;
	public const short max = 10;
	
	public short modification;
}