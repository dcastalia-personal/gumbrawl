namespace Networking.Connection {

using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Multiplayer.Playmode;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

using static Unity.Entities.SystemAPI;

[UpdateInGroup( typeof(InitAfterSceneSysGroup) )]
public partial struct InitConnectorSys : ISystem {

    EntityQuery query;

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Connector, RequireInit>() );
        state.RequireForUpdate( query );
    }

    public void OnUpdate( ref SystemState state ) {
#if UNITY_EDITOR
        if( ConnectLocallyIfOffline.settings.current.overrideStartSceneIndex > 0 ) {
            
            var connectorEntity = GetSingletonEntity<Connector>();
            var connector = GetComponent<Connector>( connectorEntity );

            var playerTags = CurrentPlayer.ReadOnlyTags();
            if( playerTags.Contains( "Host" ) ) {
                ConnectorSys.SetupLocalNetwork();
                SceneSystem.LoadSceneAsync( ClientServerBootstrap.ClientWorld.Unmanaged, connector.onlineScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
                SceneSystem.LoadSceneAsync( ClientServerBootstrap.ServerWorld.Unmanaged, connector.onlineScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
            }
            else if( playerTags.Contains( "Client" ) ) {
                Debug.Log( $"Client is trying to connect over local network" );
                ConnectorSys.ConnectOverLocalNetwork( ConnectorSys.defaultAddress );
                SceneSystem.LoadSceneAsync( ClientServerBootstrap.ClientWorld.Unmanaged, connector.onlineScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
            }

            // Debug.Log( $"Auto-loading" );
            state.EntityManager.SetComponentEnabled<Destroy>( connectorEntity, true );
            // var loadop = SceneManager.LoadSceneAsync( connector.onlineScene.ToString(), LoadSceneMode.Additive );
            // loadop!.completed += asyncOp => SceneManager.UnloadSceneAsync( SceneManager.GetActiveScene().buildIndex );
            return;
        }
#endif
        
        foreach( var connector in Query<RefRW<Connector>>().WithAll<RequireInit>() ) {
            var uiInstance = Object.Instantiate( connector.ValueRO.uiObjPrefabRef.Value ).GetComponent<ConnectorUI>();
            connector.ValueRW.uiObjInstance = new UnityObjectRef<ConnectorUI> { Value = uiInstance };
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ConnectorSys : SystemBase {
    public const ushort defaultPort = 7979;
    public const string defaultAddress = "127.0.0.1";

    ConnectionState connectionState;
    HostConnectionSys _mHostConnectionSysSystem;
    ClientConnectionSys m_HostClientSystem;

    ConnectorUI ui;
    EntityQuery connectorQuery;

    enum ConnectionState {
        Unknown,
        SetupHost,
        SetupClient,
        JoinGame,
        JoinLocalGame,
    }
    
    protected override void OnCreate() {
        connectorQuery = new EntityQueryBuilder(Allocator.Temp ).WithAll<Connector>().Build( EntityManager );
        RequireForUpdate( connectorQuery );
    }

    public void StartClientServer() {
        if( ui && ui.useRelayToggle.isOn ) {
            connectionState = ConnectionState.SetupHost;
            return;
        }

        if( ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer ) {
            Debug.LogError( $"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}" );
            return;
        }

        SetupLocalNetwork();
        LoadMatchSettingsScene();
    }

    public static void SetupLocalNetwork() {
        Debug.Log( $"Setting up local network" );
        
        var server = ClientServerBootstrap.CreateServerWorld( "ServerWorld" );
        var client = ClientServerBootstrap.CreateClientWorld( "ClientWorld" );

        World.DefaultGameObjectInjectionWorld = server;

        NetworkEndpoint ep = NetworkEndpoint.AnyIpv4.WithPort(defaultPort);
        {
            using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(ep);
        }

        ep = NetworkEndpoint.LoopbackIpv4.WithPort(defaultPort);
        {
            using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
        }
    }

    void LoadMatchSettingsScene() {
        var connectorEntity = connectorQuery.GetSingletonEntity();
        var connector = EntityManager.GetComponentData<Connector>( connectorEntity );

        if( ClientServerBootstrap.ClientWorld != null ) {
            SceneSystem.LoadSceneAsync( ClientServerBootstrap.ClientWorld.Unmanaged, connector.onlineScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
        }
        if( ClientServerBootstrap.ServerWorld != null ) {
            SceneSystem.LoadSceneAsync( ClientServerBootstrap.ServerWorld.Unmanaged, connector.onlineScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
        }
        
        EntityManager.SetComponentEnabled<Destroy>( connectorEntity, true );
        // var loadop = SceneManager.LoadSceneAsync( connector.onlineScene.ToString(), LoadSceneMode.Additive );
        // loadop!.completed += asyncOp => SceneManager.UnloadSceneAsync( SceneManager.GetActiveScene().buildIndex );
    }

    public void ConnectToServer() {
        if( ui.useRelayToggle.isOn ) {
            connectionState = ConnectionState.SetupClient;
            return;
        }

        var address = string.IsNullOrEmpty( ui.addressInput.text ) ? defaultAddress : ui.addressInput.text;
        ConnectOverLocalNetwork( address );
        
        LoadMatchSettingsScene();
    }

    public static void ConnectOverLocalNetwork( string address ) {
        // Debug.Log($"[ConnectToServer] Called on '{ui.addressInput.text}:{defaultPort}'.");
        var client = ClientServerBootstrap.CreateClientWorld( "ClientWorld" );
        // DestroyLocalSimulationWorld();

        World.DefaultGameObjectInjectionWorld = client;
        Debug.Log( $"Default world set to {World.DefaultGameObjectInjectionWorld}" );

        var ep = NetworkEndpoint.Parse(address, defaultPort);
        {
            using var drvQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(client.EntityManager, ep);
        }
    }
    
    public static void DestroyLocalSimulationWorld() {
        // Debug.Log( $"Destroying local simulation world" );
        foreach (var world in World.All) {
            if( world.Flags == WorldFlags.Game ) {
                world.Dispose();
                break;
            }
        }
    }

    // Relay functionality
    void HostServer() {
        var world = World.All[0];
        _mHostConnectionSysSystem = world.GetOrCreateSystemManaged<HostConnectionSys>();
        var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
        simGroup.AddSystemToUpdateList( _mHostConnectionSysSystem );
    }

    void SetupClient() {
        var world = World.All[0];
        m_HostClientSystem = world.GetOrCreateSystemManaged<ClientConnectionSys>();
        var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
        simGroup.AddSystemToUpdateList( m_HostClientSystem );
    }

    void JoinAsClient() {
        SetupClient();
        m_HostClientSystem.JoinUsingCode( ui.addressInput.text );
    }

    /// <summary>
    /// Collect relay server end point from completed systems. Set up server with relay support and connect client
    /// to hosted server through relay server.
    /// Both client and server world is manually created to allow us to override the <see cref="DriverConstructor"/>.
    ///
    /// Two singleton entities are constructed with listen and connect requests. These will be executed asynchronously.
    /// Connecting to relay server will not be bound immediately. The Request structs will ensure that we
    /// continuously poll until the connection is established.
    /// </summary>
    void SetupRelayHostedServerAndConnect() {
        if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer) {
            Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
            return;
        }

        Debug.Log( $"Setting up relay hosted server" );

        var world = World.All[0];
        var relayClientData = world.GetExistingSystemManaged<ClientConnectionSys>().RelayClientData;
        var relayServerData = world.GetExistingSystemManaged<HostConnectionSys>().RelayServerData;
        var joinCode = world.GetExistingSystemManaged<HostConnectionSys>().JoinCode;

        // var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
        var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        // NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        World.DefaultGameObjectInjectionWorld ??= server;

        LoadMatchSettingsScene();
        // SceneSystem.LoadSceneAsync( server.EntityManager.WorldUnmanaged, connector.nextScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
        // EntityManager.SetComponentEnabled<Destroy>( connectorEntity, true );
        // Debug.Log( $"Setting connector to require cleanup" );

        var joinCodeEntity = server.EntityManager.CreateEntity(ComponentType.ReadOnly<JoinCode>());
        server.EntityManager.SetComponentData(joinCodeEntity, new JoinCode { Value = joinCode });
        // Debug.Log( $"Join code is {joinCode}" );

        var networkStreamEntity = server.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        server.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        server.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        // For IPC this will not work and give an error in the transport layer. For this sample we force the client to connect through the relay service.
        // For a locally hosted server, the client would need to connect to NetworkEndpoint.AnyIpv4, and the relayClientData.Endpoint in all other cases.
        client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
        // client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = NetworkStreamD });
    }

    void ConnectToRelayServer() {
        Debug.Log( $"Connecting to relay server" );
        var world = World.All[0];
        var relayClientData = world.GetExistingSystemManaged<ClientConnectionSys>().RelayClientData;

        var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), relayClientData);
        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

        //Destroy the local simulation world to avoid the game scene to be loaded into it
        //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
        //and other issues.
        // DestroyLocalSimulationWorld();
        World.DefaultGameObjectInjectionWorld ??= client;
        
        var connectorEntity = connectorQuery.GetSingletonEntity();
        var connector = EntityManager.GetComponentData<Connector>( connectorEntity );
        LoadMatchSettingsScene();

        // var connectorEntity = connectorQuery.GetSingletonEntity();
        // var connector = EntityManager.GetComponentData<Connector>( connectorEntity );
        // SceneSystem.LoadSceneAsync( EntityManager.WorldUnmanaged, connector.nextScene, new SceneSystem.LoadParameters { AutoLoad = true, Flags = SceneLoadFlags.LoadAdditive } );
        // EntityManager.SetComponentEnabled<RequireCleanup>( connectorEntity, true );
        // Debug.Log( $"Setting connector to require cleanup" );

        Debug.Log( $"Client creating network stream connection request with endpoint {relayClientData.Endpoint}" );

        var networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        // For IPC this will not work and give an error in the transport layer. For this sample we force the client to connect through the relay service.
        // For a locally hosted server, the client would need to connect to NetworkEndpoint.AnyIpv4, and the relayClientData.Endpoint in all other cases.
        client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
    }

    protected override void OnUpdate() {
        if( !ui ) {
            var connector = SystemAPI.GetSingleton<Connector>();
            ui = connector.uiObjInstance.Value;

            if( ui ) {
                ui.joinButton.onClick.AddListener( ConnectToServer );
                ui.hostButton.onClick.AddListener( StartClientServer );
            }
        }
        
        switch( connectionState ) {
            case ConnectionState.SetupHost: {
                HostServer();
                connectionState = ConnectionState.SetupClient;
                goto case ConnectionState.SetupClient;
            }
            case ConnectionState.SetupClient: {
                var isServerHostedLocally = _mHostConnectionSysSystem?.RelayServerData.Endpoint.IsValid;
                var enteredJoinCode = !string.IsNullOrEmpty(ui.addressInput.text);
                if (isServerHostedLocally.GetValueOrDefault()) {
                    Debug.Log( $"Server is hosted locally" );
                    SetupClient();
                    m_HostClientSystem.GetJoinCodeFromHost();
                    connectionState = ConnectionState.JoinLocalGame;
                    goto case ConnectionState.JoinLocalGame;
                }

                if (enteredJoinCode) {
                    JoinAsClient();
                    connectionState = ConnectionState.JoinGame;
                    goto case ConnectionState.JoinGame;
                }
                break;
            }
            case ConnectionState.JoinGame: {
                var hasClientConnectedToRelayService = m_HostClientSystem?.RelayClientData.Endpoint.IsValid;
                if (hasClientConnectedToRelayService.GetValueOrDefault())
                {
                    ConnectToRelayServer();
                    connectionState = ConnectionState.Unknown;
                }
                break;
            }
            case ConnectionState.JoinLocalGame: {
                var hasClientConnectedToRelayService = m_HostClientSystem?.RelayClientData.Endpoint.IsValid;
                if (hasClientConnectedToRelayService.GetValueOrDefault())
                {
                    SetupRelayHostedServerAndConnect();
                    connectionState = ConnectionState.Unknown;
                }
                break;
            }
            case ConnectionState.Unknown:
            default: return;
        }
    }
}

public partial struct CleanupConnectorSys : ISystem {
    EntityQuery query;

    [BurstCompile] public void OnCreate( ref SystemState state ) {
        query = state.GetEntityQuery( new EntityQueryBuilder( Allocator.Temp ).WithAll<Connector, Destroy>() );
        state.RequireForUpdate( query );
    }

    public void OnUpdate( ref SystemState state ) {
        foreach( var connector in Query<RefRO<Connector>>().WithAll<Destroy>() ) {
            var uiInstance = connector.ValueRO.uiObjInstance.Value;
            if( uiInstance == null ) continue;
            Object.Destroy( uiInstance.gameObject );
        }
    }
}

public struct JoinCode : IComponentData {
    public FixedString32Bytes Value;
}

}