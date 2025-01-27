using Unity.Entities;
using Unity.NetCode;

[GhostComponent] // a way to identify a specific entity based on its id on the server
public struct ServerIndex : IComponentData {
	[GhostField] public int value;
}