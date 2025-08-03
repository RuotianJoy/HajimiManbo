using System;
using System.Collections.Generic;
using System.Linq;
using Riptide;
using Riptide.Utils;

namespace HajimiManbo.Network
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageId : ushort
    {
        PlayerJoined = 1,
        PlayerLeft = 2,
        PlayerList = 3,
        PingRequest = 4,
        PingResponse = 5,
        UpdatePlayerName = 6,
        UpdatePlayerCharacter = 7,
        SwitchToMapGeneration = 8,
        UpdateMapSettings = 9,
        ReturnToWaitingRoom = 10,
        StartGame = 11,
        // 新增玩家同步消息
        PlayerSpawn = 12,
        PlayerTeleport = 13,
        PlayerStateSync = 14,
        ClientStateUpdate = 15, // 客户端状态更新
        ReturnToRoomFromGame = 16 // 从游戏中返回房间
    }

    /// <summary>
    /// 封装 Riptide 服务器/客户端的单例管理器。
    /// 调用 <see cref="StartServer"/> 或 <see cref="Connect"/> 后，务必在游戏循环里执行 <see cref="Update"/>。
    /// </summary>
    public class NetworkManager
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance ??= new NetworkManager();

        private Server _server;
        private Client _client;
        private readonly Dictionary<ushort, string> _players = new();
        private readonly Dictionary<ushort, string> _playerCharacters = new(); // 玩家选择的角色
        private DateTime _lastPingTime;
        private float _ping = 0f;
        private const float PingInterval = 1.0f; // 每秒ping一次
        private string _localPlayerName = "玩家"; // 本地玩家名字
        private string _localPlayerCharacter = ""; // 本地玩家选择的角色
        
        // 新增：玩家状态管理（按照Terraria模式）
        private PlayerStateManager _playerStateManager;
        private readonly Dictionary<ushort, int> _clientToSlot = new(); // 客户端ID到槽位的映射
        private DateTime _lastSyncTime = DateTime.UtcNow;
        
        // 地图设置相关
        private int _mapSize = 1; // 0=小, 1=中, 2=大
        private int _monsterDifficulty = 1; // 0=简单, 1=普通, 2=困难
        private int _monsterCount = 1; // 0=少, 1=中等, 2=多
        private bool _isInMapGeneration = false; // 是否在地图生成页面
        
        // 事件声明
        public event Action OnSwitchToMapGeneration;
        public event Action OnReturnToWaitingRoom;
        public event Action OnReturnToRoomFromGame;
        public event Action<int, HajimiManbo.World.WorldSettings> OnStartGame;
        
        /// <summary>
        /// 检查客户端是否连接到服务器
        /// </summary>
        public bool IsClientConnected => _client != null && _client.IsConnected;
        
        /// <summary>
        /// 检查是否为服务器（房主）
        /// </summary>
        public bool IsServer => _server != null && _server.IsRunning;

        private const ushort DefaultPort = 12345;
        private const string DefaultIp = "127.0.0.1";

        private NetworkManager()
        {
            // 初始化日志到控制台，方便调试
            RiptideLogger.Initialize(Console.WriteLine, true);
            
            // 初始化玩家状态管理器
            _playerStateManager = new PlayerStateManager();
        }

        #region === 服务器 ===
        public void StartServer(ushort port = DefaultPort, byte maxClients = 8)
        {
            Stop(); // 若已连接/开服，先清理
            try
            {
                _server = new Server();
                _server.ClientConnected += OnClientConnected;
                _server.ClientDisconnected += OnClientDisconnected;
                _server.Start(port, maxClients);
                _players.Clear();
                _playerCharacters.Clear();
                _clientToSlot.Clear();
                _playerStateManager.Clear();
                
                // 为主机分配槽位0
                _players[0] = _localPlayerName;
                _playerCharacters[0] = _localPlayerCharacter;
                _clientToSlot[0] = 0;
                _playerStateManager.AllocateSlot(0, _localPlayerName, _localPlayerCharacter);
                
                Console.WriteLine($"[Server] Started on port {port}，等待客户端连接……");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Failed to start: {ex.Message}");
                _server = null;
            }
        }
        
        private void OnClientConnected(object sender, ServerConnectedEventArgs e)
        {
            var clientId = e.Client.Id;
            var defaultName = $"Player {clientId}";
            
            // 分配槽位
            int slot = _playerStateManager.AllocateSlot(clientId, defaultName, "");
            if (slot >= 0)
            {
                _players[clientId] = defaultName;
                _playerCharacters[clientId] = "";
                _clientToSlot[clientId] = slot;
                
                Console.WriteLine($"[Server] Client {clientId} connected, allocated slot {slot}");
                BroadcastPlayerList();
                
                // 不立即发送spawn消息，等待客户端发送角色信息后再发送
            }
            else
            {
                Console.WriteLine($"[Server] No available slots for client {clientId}");
                _server.DisconnectClient(e.Client);
            }
        }
        
        private void OnClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            var clientId = e.Client.Id;
            
            // 释放槽位
            if (_clientToSlot.TryGetValue(clientId, out int slot))
            {
                _playerStateManager.FreeSlot(slot);
                _clientToSlot.Remove(clientId);
                Console.WriteLine($"[Server] Client {clientId} disconnected, freed slot {slot}");
            }
            
            _players.Remove(clientId);
            _playerCharacters.Remove(clientId);
            BroadcastPlayerList();
        }
        
        private void BroadcastPlayerList()
        {
            if (_server == null) return;
            
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.PlayerList);
            message.AddUShort((ushort)_players.Count);
            foreach (var kvp in _players)
            {
                message.AddString(kvp.Value); // 玩家名字
                message.AddString(_playerCharacters.ContainsKey(kvp.Key) ? _playerCharacters[kvp.Key] : ""); // 角色名字
            }
            _server.SendToAll(message);
        }
        #endregion

        #region === 客户端 ===
        public void Connect(string ip = DefaultIp, ushort port = DefaultPort)
        {
            Stop(); // 若已连接/开服，先清理
            try
            {
                _client = new Client();
                _client.Connected += OnConnected;
                _client.Disconnected += OnDisconnected;
                _client.Connect($"{ip}:{port}");
                Console.WriteLine($"[Client] Connecting to {ip}:{port} …");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Connection failed: {ex.Message}");
                _client = null;
            }
        }
        
        private void OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("[Client] Connected to server");
            Console.WriteLine($"[Client] Local player character: {_localPlayerCharacter}");
            
            // 如果已经选择了角色，立即发送给服务器
            if (!string.IsNullOrEmpty(_localPlayerCharacter))
            {
                Console.WriteLine($"[Client] Sending character info to server: {_localPlayerCharacter}");
                Message message = Message.Create(MessageSendMode.Reliable, MessageId.UpdatePlayerCharacter);
                message.AddString(_localPlayerCharacter);
                _client.Send(message);
            }
        }
        
        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine("[Client] Disconnected from server");
            _players.Clear();
            _playerCharacters.Clear();
        }
        
        [MessageHandler((ushort)MessageId.PlayerList)]
         private static void HandlePlayerList(Message message)
         {
             ushort playerCount = message.GetUShort();
             Instance._players.Clear();
             Instance._playerCharacters.Clear();
             for (int i = 0; i < playerCount; i++)
             {
                 string playerName = message.GetString();
                 string characterName = message.GetString();
                 Instance._players[(ushort)i] = playerName;
                 Instance._playerCharacters[(ushort)i] = characterName;
             }
         }
         
         [MessageHandler((ushort)MessageId.PingRequest)]
         private static void HandlePingRequest(ushort fromClientId, Message message)
         {
             // 服务器收到ping请求，立即回复
             long timestamp = message.GetLong();
             Message response = Message.Create(MessageSendMode.Unreliable, MessageId.PingResponse);
             response.AddLong(timestamp);
             Instance._server?.Send(response, fromClientId);
         }
         
         [MessageHandler((ushort)MessageId.PingResponse)]
         private static void HandlePingResponse(Message message)
         {
             // 客户端收到ping回复，计算延迟
             long sentTime = message.GetLong();
             long currentTime = DateTime.Now.Ticks;
             float pingMs = (currentTime - sentTime) / 10000f; // 转换为毫秒
             Instance._ping = pingMs;
         }
         
         [MessageHandler((ushort)MessageId.UpdatePlayerName)]
         private static void HandleUpdatePlayerName(ushort fromClientId, Message message)
         {
             // 服务器收到客户端的名字更新请求
             string newName = message.GetString();
             Instance._players[fromClientId] = newName;
             Console.WriteLine($"[Server] Client {fromClientId} changed name to: {newName}");
             Instance.BroadcastPlayerList();
         }
         
         [MessageHandler((ushort)MessageId.UpdatePlayerCharacter)]
        private static void HandleUpdatePlayerCharacter(ushort fromClientId, Message message)
        {
            // 服务器收到客户端的角色更新请求
            string characterName = message.GetString();
            Instance._playerCharacters[fromClientId] = characterName;
            Console.WriteLine($"[Server] Client {fromClientId} selected character: {characterName}");
            
            // 如果客户端已经有槽位，更新槽位中的角色信息
            if (Instance._clientToSlot.TryGetValue(fromClientId, out int slot))
            {
                var playerState = Instance._playerStateManager.GetPlayerState(slot);
                if (playerState != null && playerState.Active)
                {
                    playerState.CharacterName = characterName;
                    Console.WriteLine($"[Server] Updated character for slot {slot} to {characterName}");
                    
                    // 发送spawn消息给所有客户端，通知新玩家加入
                    Instance.SendPlayerSpawn(fromClientId, slot);
                    
                    // 同时向新连接的客户端发送所有现有玩家的spawn消息
                    Instance.SendExistingPlayersToClient(fromClientId);
                }
            }
            
            Instance.BroadcastPlayerList();
        }
         
         [MessageHandler((ushort)MessageId.SwitchToMapGeneration)]
         private static void HandleSwitchToMapGeneration(Message message)
         {
             // 客户端收到切换到地图生成页面的消息
             Instance._isInMapGeneration = true;
             Console.WriteLine("[Client] Switching to map generation page");
             // 这里需要通知Game1切换状态，可以通过事件或回调实现
             Instance.OnSwitchToMapGeneration?.Invoke();
         }
         
         [MessageHandler((ushort)MessageId.UpdateMapSettings)]
         private static void HandleUpdateMapSettings(Message message)
         {
             // 客户端收到地图设置更新消息
             Instance._mapSize = message.GetInt();
             Instance._monsterDifficulty = message.GetInt();
             Instance._monsterCount = message.GetInt();
             Console.WriteLine($"[Client] Map settings updated: Size={Instance._mapSize}, Difficulty={Instance._monsterDifficulty}, Count={Instance._monsterCount}");
         }
         
         [MessageHandler((ushort)MessageId.ReturnToWaitingRoom)]
        private static void HandleReturnToWaitingRoom(Message message)
        {
            // 客户端收到返回等待房间的消息
            Instance._isInMapGeneration = false;
            Console.WriteLine("[Client] Returning to waiting room");
            Instance.OnReturnToWaitingRoom?.Invoke();
        }
        
        [MessageHandler((ushort)MessageId.ReturnToRoomFromGame)]
        private static void HandleReturnToRoomFromGame(Message message)
        {
            // 客户端收到从游戏返回房间的消息
            Console.WriteLine("[Client] Returning to room from game");
            Instance.OnReturnToRoomFromGame?.Invoke();
        }
        
        [MessageHandler((ushort)MessageId.StartGame)]
         private static void HandleStartGame(Message message)
         {
             // 客户端收到开始游戏的消息
             int seed = message.GetInt();
             int mapSize = message.GetInt();
             int monsterDifficulty = message.GetInt();
             int monsterCount = message.GetInt();
             
             var worldSettings = new HajimiManbo.World.WorldSettings
             {
                 MapSize = mapSize,
                 MonsterDifficulty = monsterDifficulty,
                 MonsterCount = monsterCount
             };
             
             Console.WriteLine($"[Client] Starting game with seed {seed}");
             Instance.OnStartGame?.Invoke(seed, worldSettings);
         }
        #endregion

        /// <summary>
        /// 每帧调用以驱动网络消息循环。
        /// </summary>
        public void Update()
        {
            try
            {
                _server?.Update();
                _client?.Update();
                
                // 服务器端定期同步玩家状态
                if (_server != null && _server.IsRunning)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _lastSyncTime).TotalSeconds >= 1.0f / 20.0f) // 20Hz同步频率
                    {
                        SyncPlayerStates();
                        _lastSyncTime = now;
                    }
                }
                
                // 客户端定期发送ping请求
                if (_client != null && _client.IsConnected)
                {
                    var now = DateTime.Now;
                    if ((now - _lastPingTime).TotalSeconds >= PingInterval)
                    {
                        SendPingRequest();
                        _lastPingTime = now;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Network] Update error: {ex.Message}");
            }
        }
        
        private void SendPingRequest()
        {
            if (_client == null || !_client.IsConnected) return;
            
            Message message = Message.Create(MessageSendMode.Unreliable, MessageId.PingRequest);
            message.AddLong(DateTime.Now.Ticks);
            _client.Send(message);
        }

        /// <summary>
        /// 停止服务器/断开客户端。
        /// </summary>
        public void Stop()
        {
            if (_client != null)
            {
                _client.Connected -= OnConnected;
                _client.Disconnected -= OnDisconnected;
                _client.Disconnect();
                _client = null;
            }

            if (_server != null)
            {
                _server.ClientConnected -= OnClientConnected;
                _server.ClientDisconnected -= OnClientDisconnected;
                _server.Stop();
                _server = null;
            }
            
            _players.Clear();
            _playerCharacters.Clear();
        }

        /// <summary>
        /// 获取等待房间的玩家名列表
        /// </summary>
        public string[] GetPlayerNames()
        {
            return _players.Values.ToArray();
        }
        
        /// <summary>
        /// 获取玩家信息列表（包含名字和角色）
        /// </summary>
        public (string name, string character)[] GetPlayerInfo()
        {
            var result = new List<(string, string)>();
            foreach (var kvp in _players)
            {
                string character = _playerCharacters.ContainsKey(kvp.Key) ? _playerCharacters[kvp.Key] : "";
                result.Add((kvp.Value, character));
            }
            return result.ToArray();
        }

        public bool IsClient => _client != null;
        public bool IsConnected => _client != null && _client.IsConnected;
        public float Ping => _ping;
        
        /// <summary>
        /// 设置本地玩家名字
        /// </summary>
        public void SetLocalPlayerName(string name)
        {
            _localPlayerName = name;
            
            // 如果是服务器，更新Host名字
            if (IsServer)
            {
                _players[0] = _localPlayerName;
                BroadcastPlayerList();
            }
            // 如果是客户端，发送名字更新消息给服务器
            else if (IsConnected)
            {
                Message message = Message.Create(MessageSendMode.Reliable, MessageId.UpdatePlayerName);
                message.AddString(_localPlayerName);
                _client.Send(message);
            }
        }
        
        /// <summary>
        /// 获取本地玩家名字
        /// </summary>
        public string GetLocalPlayerName()
        {
            return _localPlayerName;
        }
        
        /// <summary>
        /// 设置本地玩家选择的角色
        /// </summary>
        public void SetLocalPlayerCharacter(string characterName)
        {
            _localPlayerCharacter = characterName;
            
            // 如果是服务器，更新Host角色
            if (IsServer)
            {
                _playerCharacters[0] = _localPlayerCharacter;
                
                // 更新PlayerStateManager中的角色信息
                var playerState = _playerStateManager.GetPlayerState(0);
                if (playerState != null && playerState.Active)
                {
                    playerState.CharacterName = characterName;
                    // 为主机发送PlayerSpawn消息
                    SendPlayerSpawn(0, 0);
                }
                
                BroadcastPlayerList();
            }
            // 如果是客户端，发送角色更新消息给服务器
            else if (IsConnected)
            {
                Message message = Message.Create(MessageSendMode.Reliable, MessageId.UpdatePlayerCharacter);
                message.AddString(_localPlayerCharacter);
                _client.Send(message);
            }
        }
        
        /// <summary>
        /// 获取本地玩家选择的角色
        /// </summary>
        public string GetLocalPlayerCharacter()
        {
            return _localPlayerCharacter;
        }
        
        /// <summary>
        /// 服务器通知所有客户端切换到地图生成页面
        /// </summary>
        public void BroadcastSwitchToMapGeneration()
        {
            if (!IsServer) return;
            
            _isInMapGeneration = true;
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.SwitchToMapGeneration);
            _server.SendToAll(message);
            Console.WriteLine("[Server] Broadcasting switch to map generation page");
        }
        
        /// <summary>
        /// 服务器同步地图设置到所有客户端
        /// </summary>
        public void BroadcastMapSettings(int mapSize, int monsterDifficulty, int monsterCount)
        {
            if (!IsServer) return;
            
            _mapSize = mapSize;
            _monsterDifficulty = monsterDifficulty;
            _monsterCount = monsterCount;
            
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.UpdateMapSettings);
            message.AddInt(mapSize);
            message.AddInt(monsterDifficulty);
            message.AddInt(monsterCount);
            _server.SendToAll(message);
            Console.WriteLine($"[Server] Broadcasting map settings: Size={mapSize}, Difficulty={monsterDifficulty}, Count={monsterCount}");
        }
        
        /// <summary>
        /// 获取当前地图设置
        /// </summary>
        public (int mapSize, int monsterDifficulty, int monsterCount) GetMapSettings()
        {
            return (_mapSize, _monsterDifficulty, _monsterCount);
        }
        
        /// <summary>
        /// 检查是否在地图生成页面
        /// </summary>
        public bool IsInMapGeneration => _isInMapGeneration;
        
        /// <summary>
        /// 广播返回等待房间消息
        /// </summary>
        public void BroadcastReturnToWaitingRoom()
        {
            if (!IsServer) return;
            
            _isInMapGeneration = false;
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.ReturnToWaitingRoom);
            _server.SendToAll(message);
            Console.WriteLine("[Server] Broadcasting return to waiting room");
        }
        
        /// <summary>
        /// 广播从游戏返回房间消息
        /// </summary>
        public void BroadcastReturnToRoomFromGame()
        {
            if (!IsServer) return;
            
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.ReturnToRoomFromGame);
            _server.SendToAll(message);
            Console.WriteLine("[Server] Broadcasting return to room from game");
        }
        
        /// <summary>
        /// 广播开始游戏消息
        /// </summary>
        public void BroadcastStartGame(int seed, HajimiManbo.World.WorldSettings worldSettings)
        {
            if (!IsServer) return;
            
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.StartGame);
            message.AddInt(seed);
            var (width, height) = worldSettings.GetWorldSize();
            message.AddInt(width);
            message.AddInt(height);
            message.AddInt(worldSettings.MonsterDifficulty);
            message.AddInt(worldSettings.MonsterCount);
            _server.SendToAll(message);
            Console.WriteLine($"[Server] Broadcasting start game with seed {seed}");
        }
        
        #region === 玩家状态同步（按照Terraria模式） ===
        
        /// <summary>
        /// 发送玩家spawn消息
        /// </summary>
        private void SendPlayerSpawn(ushort clientId, int slot)
        {
            if (!IsServer) return;
            
            var playerState = _playerStateManager.GetPlayerState(slot);
            if (playerState == null || !playerState.Active) return;
            
            // 计算spawn位置（世界左边） - 所有玩家使用相同的spawn逻辑
            var spawnX = 10 * 16; // 距离左边界10个tile
            var spawnY = 0; // 初始Y位置，实际会在客户端根据地形计算
            
            // 设置spawn位置和当前位置
            playerState.SpawnPosition = new Microsoft.Xna.Framework.Vector2(spawnX, spawnY);
            playerState.Position = playerState.SpawnPosition;
            
            Console.WriteLine($"[Server] Setting spawn position for slot {slot}: ({spawnX}, {spawnY})");
            
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.PlayerSpawn);
            message.AddInt(slot);
            message.AddUShort(clientId); // 添加客户端ID用于识别
            message.AddString(playerState.PlayerName);
            message.AddString(playerState.CharacterName);
            message.AddFloat(spawnX);
            message.AddFloat(spawnY);
            
            // 广播给所有客户端（包括发送者）
            _server.SendToAll(message);
            
            Console.WriteLine($"[Server] Sent spawn message for slot {slot} (clientId {clientId}) at ({spawnX}, {spawnY})");
        }
        
        /// <summary>
        /// 向新连接的客户端发送所有现有玩家的spawn消息
        /// </summary>
        private void SendExistingPlayersToClient(ushort newClientId)
        {
            if (!IsServer) return;
            
            // 遍历所有客户端到槽位的映射
            foreach (var kvp in _clientToSlot)
            {
                ushort existingClientId = kvp.Key;
                int slot = kvp.Value;
                
                // 跳过新连接的客户端自己
                if (existingClientId == newClientId)
                    continue;
                
                var playerState = _playerStateManager.GetPlayerState(slot);
                if (playerState != null && playerState.Active)
                {
                    // 发送现有玩家的spawn消息给新客户端
                    Message message = Message.Create(MessageSendMode.Reliable, MessageId.PlayerSpawn);
                    message.AddInt(slot);
                    message.AddUShort(existingClientId); // 添加客户端ID
                    message.AddString(playerState.PlayerName);
                    message.AddString(playerState.CharacterName);
                    message.AddFloat(playerState.Position.X);
                    message.AddFloat(playerState.Position.Y);
                    
                    _server.Send(message, newClientId);
                    Console.WriteLine($"[Server] Sent existing player spawn (slot {slot}, clientId {existingClientId}) to new client {newClientId}");
                }
            }
        }
        
        /// <summary>
        /// 更新玩家状态（由游戏逻辑调用）
        /// </summary>
        public void UpdatePlayerState(ushort clientId, Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Vector2 velocity, bool isMoving, bool isSprinting, bool isOnGround, bool facingRight)
        {
            // 服务器端：直接更新PlayerStateManager
            if (IsServer)
            {
                // 服务器主机总是槽位0
                int slot = 0;
                var playerState = _playerStateManager.GetPlayerState(slot);
                if (playerState != null && playerState.Active)
                {
                    playerState.Position = position;
                    playerState.Velocity = velocity;
                    playerState.IsMoving = isMoving;
                    playerState.IsSprinting = isSprinting;
                    playerState.IsOnGround = isOnGround;
                    playerState.FacingRight = facingRight;
                }
            }
            // 客户端：发送状态更新给服务器
            else if (IsClientConnected && _client != null)
            {
                int localSlot = GetLocalPlayerSlot();
                if (localSlot >= 0)
                {
                    Message message = Message.Create(MessageSendMode.Unreliable, MessageId.ClientStateUpdate);
                    message.AddInt(localSlot);
                    message.AddFloat(position.X);
                    message.AddFloat(position.Y);
                    message.AddFloat(velocity.X);
                    message.AddFloat(velocity.Y);
                    message.AddBool(isMoving);
                    message.AddBool(isSprinting);
                    message.AddBool(isOnGround);
                    message.AddBool(facingRight);
                    _client.Send(message);
                }
            }
        }
        
        /// <summary>
        /// 同步玩家状态（定时调用）
        /// </summary>
        public void SyncPlayerStates()
        {
            if (!IsServer) return;
            
            var playersNeedingSync = _playerStateManager.GetPlayersNeedingSync();
            if (playersNeedingSync.Count == 0) return;
            
            foreach (var (slot, playerState) in playersNeedingSync)
            {
                Message message = Message.Create(MessageSendMode.Unreliable, MessageId.PlayerStateSync);
                message.AddInt(slot);
                message.AddFloat(playerState.Position.X);
                message.AddFloat(playerState.Position.Y);
                message.AddFloat(playerState.Velocity.X);
                message.AddFloat(playerState.Velocity.Y);
                message.AddBool(playerState.IsMoving);
                message.AddBool(playerState.IsSprinting);
                message.AddBool(playerState.IsOnGround);
                message.AddBool(playerState.FacingRight);
                
                _server.SendToAll(message);
                playerState.MarkSynced();
            }
        }
        
        /// <summary>
        /// 获取玩家状态管理器（供游戏逻辑使用）
        /// </summary>
        public PlayerStateManager GetPlayerStateManager()
        {
            return _playerStateManager;
        }
        
        /// <summary>
        /// 获取本地玩家的槽位
        /// </summary>
        public int GetLocalPlayerSlot()
        {
            if (IsServer)
            {
                return 0; // 主机总是槽位0
            }
            else if (IsClientConnected && _client != null)
            {
                return _clientToSlot.GetValueOrDefault(_client.Id, -1);
            }
            return -1;
        }
        
        #endregion
        
        #region === 消息处理器 ===
        
        [MessageHandler((ushort)MessageId.PlayerSpawn)]
        private static void HandlePlayerSpawn(Message message)
        {
            int slot = message.GetInt();
            ushort clientId = message.GetUShort(); // 读取客户端ID
            string playerName = message.GetString();
            string characterName = message.GetString();
            float spawnX = message.GetFloat();
            float spawnY = message.GetFloat();
            
            Console.WriteLine($"[Client] Received player spawn: slot {slot}, clientId {clientId}, name {playerName}, character {characterName}, pos ({spawnX}, {spawnY})");
            
            // 如果是客户端，通过客户端ID准确识别自己的槽位
            if (Instance.IsClient && !Instance.IsServer)
            {
                if (Instance._client != null && clientId == Instance._client.Id)
                {
                    // 记录自己的槽位
                    Instance._clientToSlot[Instance._client.Id] = slot;
                    Console.WriteLine($"[Client] Identified local player slot: {slot} for clientId {clientId}");
                }
            }
            
            // 更新本地的玩家状态管理器
            var playerState = Instance._playerStateManager.GetPlayerState(slot);
            if (playerState == null || !playerState.Active)
            {
                // 如果槽位为空，直接在指定槽位设置玩家状态
                playerState = Instance._playerStateManager.GetPlayerState(slot);
                if (playerState != null)
                {
                    playerState.Active = true;
                    playerState.PlayerId = clientId;
                    playerState.PlayerName = playerName;
                    playerState.CharacterName = characterName;
                    playerState.LastUpdateTime = DateTime.UtcNow;
                }
            }
            
            if (playerState != null)
            {
                playerState.Position = new Microsoft.Xna.Framework.Vector2(spawnX, spawnY);
                playerState.SpawnPosition = new Microsoft.Xna.Framework.Vector2(spawnX, spawnY);
            }
        }
        
        [MessageHandler((ushort)MessageId.PlayerStateSync)]
        private static void HandlePlayerStateSync(Message message)
        {
            int slot = message.GetInt();
            float posX = message.GetFloat();
            float posY = message.GetFloat();
            float velX = message.GetFloat();
            float velY = message.GetFloat();
            bool isMoving = message.GetBool();
            bool isSprinting = message.GetBool();
            bool isOnGround = message.GetBool();
            bool facingRight = message.GetBool();
            
            // 更新本地的玩家状态副本
            var playerState = Instance._playerStateManager.GetPlayerState(slot);
            if (playerState != null && playerState.Active)
            {
                playerState.Position = new Microsoft.Xna.Framework.Vector2(posX, posY);
                playerState.Velocity = new Microsoft.Xna.Framework.Vector2(velX, velY);
                playerState.IsMoving = isMoving;
                playerState.IsSprinting = isSprinting;
                playerState.IsOnGround = isOnGround;
                playerState.FacingRight = facingRight;
            }
        }
        
        [MessageHandler((ushort)MessageId.ClientStateUpdate)]
        private static void HandleClientStateUpdate(ushort fromClientId, Message message)
        {
            // 服务器收到客户端状态更新
            int slot = message.GetInt();
            float posX = message.GetFloat();
            float posY = message.GetFloat();
            float velX = message.GetFloat();
            float velY = message.GetFloat();
            bool isMoving = message.GetBool();
            bool isSprinting = message.GetBool();
            bool isOnGround = message.GetBool();
            bool facingRight = message.GetBool();
            
            // 更新服务器端的PlayerStateManager
            var playerState = Instance._playerStateManager.GetPlayerState(slot);
            if (playerState != null && playerState.Active)
            {
                playerState.Position = new Microsoft.Xna.Framework.Vector2(posX, posY);
                playerState.Velocity = new Microsoft.Xna.Framework.Vector2(velX, velY);
                playerState.IsMoving = isMoving;
                playerState.IsSprinting = isSprinting;
                playerState.IsOnGround = isOnGround;
                playerState.FacingRight = facingRight;
                
                // 立刻广播PlayerStateSync给所有客户端
                Message syncMessage = Message.Create(MessageSendMode.Unreliable, MessageId.PlayerStateSync);
                syncMessage.AddInt(slot);
                syncMessage.AddFloat(posX);
                syncMessage.AddFloat(posY);
                syncMessage.AddFloat(velX);
                syncMessage.AddFloat(velY);
                syncMessage.AddBool(isMoving);
                syncMessage.AddBool(isSprinting);
                syncMessage.AddBool(isOnGround);
                syncMessage.AddBool(facingRight);
                
                Instance._server?.SendToAll(syncMessage);
            }
        }
        
        #endregion
    }
}