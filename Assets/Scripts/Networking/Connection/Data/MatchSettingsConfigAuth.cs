using Unity.Entities;
using UnityEngine;

public class MatchSettingsConfigAuth : MonoBehaviour {
	public GameObject matchSettingsUIPrefab;
	public GameObject enterGameRPC;
	public GameObject matchSettingsPrefab;

	public class Baker : Baker<MatchSettingsConfigAuth> {

		public override void Bake( MatchSettingsConfigAuth configAuth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new MatchSettingsConfig {
				matchSettingsUIPrefab = new UnityObjectRef<GameObject> { Value = configAuth.matchSettingsUIPrefab }, 
				enterGameRPC = GetEntity( configAuth.enterGameRPC, TransformUsageFlags.None ),
				matchSettingsPrefab = GetEntity( configAuth.matchSettingsPrefab, TransformUsageFlags.None ),
			} );
		}
	}
}

public struct MatchSettingsConfig : IComponentData {
	public UnityObjectRef<GameObject> matchSettingsUIPrefab;
	public UnityObjectRef<MatchSettingsUI> matchSettingsUIInstance;

	public Entity enterGameRPC;
	public Entity matchSettingsPrefab;
}