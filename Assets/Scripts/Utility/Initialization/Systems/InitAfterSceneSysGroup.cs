using Unity.Entities;
using Unity.Scenes;

[UpdateInGroup(typeof(InitializationSystemGroup))] [UpdateAfter(typeof(SceneSystemGroup))]
public partial class InitAfterSceneSysGroup : ComponentSystemGroup {}