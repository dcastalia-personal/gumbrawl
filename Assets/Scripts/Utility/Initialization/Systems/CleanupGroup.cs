using Unity.Entities;

[UpdateInGroup(typeof(EndSimulationSyncGroup))]
public partial class CleanupGroup : ComponentSystemGroup {}