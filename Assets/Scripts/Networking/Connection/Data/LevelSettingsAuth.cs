using Unity.Entities;
using UnityEngine;

public class LevelSettingsAuth : MonoBehaviour {
	public GameObject characterPrefab;
	public GameObject inGameUIPrefab;
	public GameObject managedPoolPrefab;
	public GameObject finishedEnteringGameRpc;
	public GameObject ballsListPrefab;
	public GameObject scoringPrefab;

	public class Baker : Baker<LevelSettingsAuth> {

		public override void Bake( LevelSettingsAuth auth ) {
			var self = GetEntity( TransformUsageFlags.None );
			AddComponent( self, new LevelSettings {
				finishedEnteringGameRpc = GetEntity( auth.finishedEnteringGameRpc, TransformUsageFlags.None ),
				characterPrefab = GetEntity( auth.characterPrefab, TransformUsageFlags.Dynamic ),
				inGameUIPrefab = new UnityObjectRef<GameObject> { Value = auth.inGameUIPrefab },
				managedPoolPrefab = new UnityObjectRef<GameObject> { Value = auth.managedPoolPrefab },
				ballsListPrefab = GetEntity( auth.ballsListPrefab, TransformUsageFlags.None ),
				scoringPrefab = GetEntity( auth.scoringPrefab, TransformUsageFlags.None ),
			} );
		}
	}
}

public struct LevelSettings : IComponentData {
	public Entity finishedEnteringGameRpc;
	public Entity characterPrefab;
	public Entity ballsListPrefab;
	public Entity scoringPrefab;

	public UnityObjectRef<GameObject> inGameUIPrefab;
	public UnityObjectRef<GameObject> managedPoolPrefab;
}