using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

public class ControlsAuth : MonoBehaviour {

	public class Baker : Baker<ControlsAuth> {

		public override void Bake( ControlsAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );

			AddComponent<Controls>( self );
			AddComponent<EatPressed>( self ); SetComponentEnabled<EatPressed>( self, false );
			AddComponent<EatReleased>( self ); SetComponentEnabled<EatReleased>( self, false );
			AddComponent<BlowPressed>( self ); SetComponentEnabled<BlowPressed>( self, false );
			AddComponent<BlowReleased>( self ); SetComponentEnabled<BlowReleased>( self, false );
			AddComponent<MovementInput>( self );
		}
	}
}

public struct Controls : IComponentData {}
public struct EatPressed : IComponentData, IEnableableComponent {}
public struct EatReleased : IComponentData, IEnableableComponent {}
public struct BlowPressed : IComponentData, IEnableableComponent {}
public struct BlowReleased : IComponentData, IEnableableComponent {}

public struct MovementInput : IComponentData {
	public float2 value;
}