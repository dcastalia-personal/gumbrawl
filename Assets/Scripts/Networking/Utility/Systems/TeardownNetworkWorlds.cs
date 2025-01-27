using Unity.NetCode;
using Unity.Scenes;
using UnityEngine;

public class TeardownNetworkWorlds : MonoBehaviour
{
	void Awake() {
		if( ClientServerBootstrap.ServerWorld != null ) ClientServerBootstrap.ServerWorld.Dispose();
		if( ClientServerBootstrap.ClientWorld != null ) ClientServerBootstrap.ClientWorld.Dispose();
	}
}
