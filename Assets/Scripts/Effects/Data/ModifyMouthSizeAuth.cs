using Unity.Entities;
using UnityEngine;

public class ModifyMouthSizeAuth : MonoBehaviour {
	public int amount;

	public class Baker : Baker<ModifyMouthSizeAuth> {

		public override void Bake( ModifyMouthSizeAuth auth ) {
			var self = GetEntity( TransformUsageFlags.Dynamic );
			AddComponent( self, new ModifyMouthSize { amount = auth.amount } );
		}
	}
}

public struct ModifyMouthSize : IComponentData {
	public const int min = 1;
	public const int max = 5;
	
	public int amount;
}