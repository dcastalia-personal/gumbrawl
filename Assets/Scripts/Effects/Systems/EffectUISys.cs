using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static Unity.Entities.SystemAPI;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DisplayEffectsSys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, GhostOwnerIsLocal, EffectDisplayReqElement>() );
		state.RequireForUpdate( query );
		state.RequireForUpdate<BallsList>();
	}

	public void OnUpdate( ref SystemState state ) {
		var ballListEntity = GetSingletonEntity<BallsList>();
		var ballList = GetBuffer<BallPrefabRef>( ballListEntity );
		var ballColorList = GetBuffer<BallColor>( ballListEntity );
		var uiPrefabRef = GetComponent<EffectUIPrefabRef>( ballListEntity );
		foreach( var displaysReqs in Query<DynamicBuffer<EffectDisplayReqElement>>().WithAll<Character, GhostOwnerIsLocal>().WithChangeFilter<EffectDisplayReqElement>() ) {

			InGameUI.current.bubbleNotificationLayout.Clear();
			foreach( var displayReq in displaysReqs ) {
				var characterEffects = GetBuffer<CharacterEffectRefElement>( ballList[ displayReq.ballPrefabIndex ].value );
				var bubbleEffects = GetBuffer<BubbleEffectRefElement>( ballList[ displayReq.ballPrefabIndex ].value );

				var effects = new NativeList<Entity>( Allocator.Temp );
				foreach( var characterEffect in characterEffects ) effects.Add( characterEffect.value );
				foreach( var bubbleEffect in bubbleEffects ) effects.Add( bubbleEffect.value );
				
				// MAJOR TODO: we need to reduce the effect display to a single prefab with configurable fields
				// TODO: then, the effect display layout can look at the contents of the EffectDisplayReqElement buffer and regenerate its (pooled) ui items from that

				foreach( var effect in effects ) {
					var effectUIManaged = state.EntityManager.GetComponentObject<EffectUIDetails>( effect );
					var ui = ((GameObject)Object.Instantiate( uiPrefabRef.value )).GetComponent<EffectDisplayEntry>();
					var origBackgroundAlpha = ui.background.color.a;
					var newBackgroundColor = ballColorList[ displayReq.ballPrefabIndex ].value;
					newBackgroundColor.w = origBackgroundAlpha;
					ui.background.color = (Vector4)newBackgroundColor;
					ui.effectTitle.text = effectUIManaged.title;
					ui.effectDescription.text = effectUIManaged.description;
				
					InGameUI.current.bubbleNotificationLayout.Add( ui.rectTransform );
				}
			}
		}
	}
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClearEffectsDisplaySys : ISystem {
	EntityQuery query;

	[BurstCompile] public void OnCreate( ref SystemState state ) {
		query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Character, GhostOwnerIsLocal, BallIndexRefElement>() );
		state.RequireForUpdate( query );
	}

	public void OnUpdate( ref SystemState state ) {
		foreach( var chewingBalls in Query<DynamicBuffer<BallIndexRefElement>>().WithAll<Character, GhostOwnerIsLocal>().WithChangeFilter<BallIndexRefElement>() ) {
			if( chewingBalls.Length == 0 ) {
				InGameUI.current.bubbleNotificationLayout.Clear();
			}
		}
	}
}