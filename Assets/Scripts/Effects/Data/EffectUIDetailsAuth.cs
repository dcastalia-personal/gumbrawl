using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class EffectUIDetailsAuth : MonoBehaviour {
	public string title;
	public string description;

	public class Baker : Baker<EffectUIDetailsAuth> {

		public override void Bake( EffectUIDetailsAuth detailsAuth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponentObject( self, new EffectUIDetails { title = detailsAuth.title, description = detailsAuth.description } );
		}
	}
}

public class EffectUIDetails : IComponentData {
	public string title;
	public string description;
}