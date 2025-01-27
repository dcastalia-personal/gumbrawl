using Unity.NetCode;
using UnityEngine;

public class RelayBootstrap : ClientServerBootstrap {
	public override bool Initialize( string defaultWorldName ) {
		if (!DetermineIfBootstrappingEnabled()) return false;
		
		CreateLocalWorld( defaultWorldName );
		return true;
	}
}
