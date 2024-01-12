using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
//封装一个结构体来定义存储用户信息便于扩展
public struct PlayerInfo : INetworkSerializable//网络序列化接口,为了让这个结构体或类能够在网络流中传递,需要使用该接口
{
    public ulong id;//玩家ID信息
    public string name;//玩家自定义姓名
    public bool isready;//玩家是否准备信息
    public int gender;//0代表男,1代表女
    //这个函数中需要填写要在网络流中序列化的字段信息
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);//结构体必须在字段前添加ref声明表示按引用传递,类则不用,因为类直接传递的引用
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref isready);
        serializer.SerializeValue(ref gender);
    }
}
public class LobbyCtrl : NetworkBehaviour//从现在开始我们进入了网络状态,所以我们要继承NetworkBehaviour
{
    [SerializeField]
    private Transform _canvas;
    private Transform _content;
    private GameObject _originCell;
    private Button _startBtn;
    private Toggle _ready;
    private TMP_InputField _name;
    private Dictionary<ulong, PlayerListCell> _cellDictionary;
    private Dictionary<ulong, PlayerInfo> _allPlayerInfos;
    /// <summary>
    /// 点击开始游戏按钮要执行的事件
    /// </summary>
    private void OnStartClick()
    {
        GameManager.Instance.LoadScene("Game");//封装的网络流载入场景,这里服务端会带着所有客户端同时同步载入"Game"场景
        GameManager.Instance.StartGame(_allPlayerInfos);
    }
    private void OnReadyToggle(bool arg0)
    {
        _cellDictionary[NetworkManager.LocalClientId].SetReady(arg0);
        UpdatePlayerInfo(NetworkManager.LocalClientId, arg0);
        if (IsServer)//如果你是服务器可以直接调用ClientRpc来通知所有客户端
        {
            UpdateAllPlayerInfos();
        }
        else//否则,你是客户端只能去通知服务器再由服务器来通知所有客户端更新信息
        {
            UpdateAllPlayerInfosServerRpc(_allPlayerInfos[NetworkManager.LocalClientId]);
        }
    }
    //简而言之：服务器可以直接通知所有客户端调用一个函数，而客户端需要先通知服务器再通过服务器通知所有客户端调用一个函数
    [ServerRpc(RequireOwnership = false)]//客户端想要通知其他客户端全部同时调用一个函数就需要先通知服务器的帮助,于是需要使用ServerRpc
    private void UpdateAllPlayerInfosServerRpc(PlayerInfo player)
    {
        //更新服务器上的PlayerInfo信息
        _allPlayerInfos[player.id] = player;
        _cellDictionary[player.id].UpdateInfo(player);
        UpdateAllPlayerInfos();//为了让服务器可以调用此函数
    }
    private void UpdatePlayerInfo(ulong id, bool isReady)//在本地更新了PlayerInfo的信息
    {
        PlayerInfo info = _allPlayerInfos[id];
        info.isready = isReady;
        _allPlayerInfos[id] = info;
    }
    /// <summary>
    /// 添加玩家进入房间
    /// </summary>
    /// <param name="playerID">玩家ID</param>
    /// <param name="isReady">是否准备</param>
    public void AddPlayer(PlayerInfo playerInfo)
    {
        _allPlayerInfos.Add(playerInfo.id, playerInfo);//首先把新加入的玩家信息存入字典中
        GameObject clone = Instantiate(_originCell);
        clone.transform.SetParent(_content, false);
        PlayerListCell cell = clone.GetComponent<PlayerListCell>();
        _cellDictionary.Add(playerInfo.id, cell);
        cell.Initial(playerInfo);
        clone.SetActive(true);
    }
    /// <summary>
    /// 当玩家从网络上生成出来(克隆)时,这是一个生命周期函数,类似于Start不过是网络版
    /// </summary>
    public override void OnNetworkSpawn()
    {
        //判断一下生成的该网络是服务器主机还是客户端
        if (IsServer)
        {
            //如果是服务器主机,则设置一下当客户端连接服务器的回调函数
            NetworkManager.OnClientConnectedCallback += OnClientConnect;
        }
        //这里需要不同的客户端同步房间里的玩家信息状态,因此需要继承NetworkBehaviour类来实现网络同步
        _allPlayerInfos = new Dictionary<ulong, PlayerInfo>();
        _content = _canvas.Find("List/Viewport/Content");
        _originCell = _content.Find("Cell").gameObject;
        _startBtn = _canvas.Find("StartBtn").GetComponent<Button>();
        _ready = _canvas.Find("Ready").GetComponent<Toggle>();
        _name = _canvas.Find("Name").GetComponent<TMP_InputField>();
        _name.onEndEdit.AddListener(OnEndEdit);//当用户结束名称编辑产生的回调事件
        _startBtn.onClick.AddListener(OnStartClick);
        _ready.onValueChanged.AddListener(OnReadyToggle);
        _cellDictionary = new Dictionary<ulong, PlayerListCell>();
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;//NetworkManager.LocalClientId是本地客户端自带的ID,先把自己添加到网络房间中
        playerInfo.name = "玩家" + playerInfo.id;//用户默认姓名"玩家"+ID
        _name.text = playerInfo.name;
        playerInfo.isready = false;
        playerInfo.gender = 0;
        //在创建网络主机后先把本地的用户传进来也就是自己
        AddPlayer(playerInfo);
        Toggle male = _canvas.Find("Gender/Male").GetComponent<Toggle>();
        Toggle female = _canvas.Find("Gender/Female").GetComponent<Toggle>();
        male.onValueChanged.AddListener(OnMaleToggle);
        female.onValueChanged.AddListener(OnFemalealeToggle);
        base.OnNetworkSpawn();
    }
    private void OnEndEdit(string arg0)
    {
        if (string.IsNullOrEmpty(arg0))//如果玩家什么都没有输入则返回Null
        {
            return;
        }
        //用户有输入我们就更新之前的名字
        //先找到自己的PlayerInfo
        PlayerInfo playerInfo = _allPlayerInfos[NetworkManager.LocalClientId];
        playerInfo.name = arg0;
        //把更新后的数据PlayerInfo再更新到字典中去(本地)
        _allPlayerInfos[NetworkManager.LocalClientId] = playerInfo;
        //更改UI文字显示
        _cellDictionary[NetworkManager.LocalClientId].UpdateInfo(playerInfo);
        //修改网络流数据
        if (IsServer)
        {
            UpdateAllPlayerInfos();
        }
        else
        {
            UpdateAllPlayerInfosServerRpc(playerInfo);
        }
    }
    private void OnMaleToggle(bool arg0)
    {
        if (arg0)
        {
            PlayerInfo playerInfo = _allPlayerInfos[NetworkManager.LocalClientId];
            playerInfo.gender = 0;
            //更新本地的信息
            _allPlayerInfos[NetworkManager.LocalClientId] = playerInfo;
            _cellDictionary[NetworkManager.LocalClientId].UpdateInfo(playerInfo);
            //更新网络全局信息
            if (IsServer)
            {
                //如果是服务器直接调用ClientRpc更新所有客户端信息
                UpdateAllPlayerInfos();
            }
            else
            {
                //客户端通知服务器去调用ClientRpc更新所有客户端信息
                UpdateAllPlayerInfosServerRpc(playerInfo);
            }
            BodyCtrl.Instance.SwitchGender(0);
        }
    }
    private void OnFemalealeToggle(bool arg0)
    {
        if (arg0)
        {
            PlayerInfo playerInfo = _allPlayerInfos[NetworkManager.LocalClientId];
            playerInfo.gender = 1;
            //更新本地的信息
            _allPlayerInfos[NetworkManager.LocalClientId] = playerInfo;
            _cellDictionary[NetworkManager.LocalClientId].UpdateInfo(playerInfo);
            //更新网络全局信息
            if (IsServer)
            {
                //如果是服务器直接调用ClientRpc更新所有客户端信息
                UpdateAllPlayerInfos();
            }
            else
            {
                //客户端通知服务器去调用ClientRpc更新所有客户端信息
                UpdateAllPlayerInfosServerRpc(playerInfo);
            }
            BodyCtrl.Instance.SwitchGender(1);
        }
    }
    /// <summary>
    /// 当新的客户端连接的回调函数,它是由服务器控制的
    /// </summary>
    /// <param name="playerid"></param>
    private void OnClientConnect(ulong playerid)
    {
        //当新的客户端连接时,实例化新的玩家信息并抓取他的id信息
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = playerid;
        playerInfo.name = "玩家" + playerid;
        playerInfo.isready = false;
        //然后将该玩家加入到房间中
        AddPlayer(playerInfo);
        UpdateAllPlayerInfos();
    }
    /// <summary>
    /// 更新房间中所有玩家信息
    /// </summary>
    private void UpdateAllPlayerInfos()//此函数只有服务端能调用其中的内容
    {
        bool canGo = true;
        //通知其他所有的客户端,把它们没有的Player信息添加进去,让每一个客户端都遍历一遍所有玩家信息
        foreach (var item in _allPlayerInfos)
        {
            if (!item.Value.isready)
            {
                canGo = false;
            }
            UpdatePlayerInfoClientRpc(item.Value);//此时,向所有的客户端发送Rpc让他们更新玩家信息列表
        }
        _startBtn.gameObject.SetActive(canGo);
    }
    //此标签表示所有的！客户端的LobbyCtrl会调用此段函数
    [ClientRpc]//但是ClientRpc也会在当主机的玩家中也调用一下因为Host是服务器+客户端这么一个角色，所有它也会执行客户端Rpc，如果是Server角色的话它就不会调用
    private void UpdatePlayerInfoClientRpc(PlayerInfo playerInfo)
    {
        if (!IsServer)
        {
            if (_allPlayerInfos.ContainsKey(playerInfo.id))//先判断字典中是否有玩家ID
            {
                _allPlayerInfos[playerInfo.id] = playerInfo;//有就更新一下客户端玩家信息
            }
            else
            {
                AddPlayer(playerInfo);//没有就在客户端重新创建构建客户端玩家信息
            }
            UpdatePlayerCells();//更改UI的实际显示,之前只是更改了数据
        }
    }
    private void UpdatePlayerCells()
    {
        foreach (var item in _allPlayerInfos)
        {
            //_cellDictionary[item.Key].SetReady(item.Value.isready);//更改Cell格子的文字显示
            _cellDictionary[item.Key].UpdateInfo(item.Value);
        }
    }
}
