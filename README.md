===================================================================================================================================================================
1.初始化项目

先点击Window -> PackeManager ->安装插件:NetCode for GameObjects

通常需要创建四个场景搭成一个基础的多人网络游戏框架Init(初始化) Start(开始) Lobby(大厅) Game(游戏)

在初始化场景中，创建一个空节点，名为：NetworkManager并且AddComponent：Network Manager
Network Manager就是网络管理者，创建它之后需要设定一下它其中必要的字段属性：

①Player Prefab：需要往此字段拖入玩家预制体，这个预制体代表每一个玩家，并且该预制体上必须要有组件：Network Object
②Network Prefabs Lists：此字段需要在Project面板右键Create，然后鼠标点击Netcode选项卡，创建NetworkPrefabsList文件，将该文件拖入此字段即可（注意！一定要将Netcode for GameObjects插件版本更新到1.5.2以上才能在Project中直接创建此文件！）
③Network Transport：在此字段的下面有一个选项卡：Select transport，点击之后选择：Unity Transport就会自动设置该字段属性

在初始化场景中再创建一个空节点：GameManager
然后创建一个脚本文件：GameManager.cs拖拽至空节点上
GameManager.cs内容：

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        DontDestroyOnLoad(gameObject);//将此游戏对象在场景切换时保留,当前的游戏对象即便它所在的场景被销毁,但是它会被自动传入到系统默认生成的新场景中而不会被销毁
        SceneManager.LoadScene(1);//载入Start场景
    }
}
此端内容实现了简单的场景初始化
===================================================================================================================================================================

===================================================================================================================================================================
2.建立连接

在Start场景中，创建UI -> Button，选择Button按下F2重命名为CreateBtn，这个Button是我们用来创建主机的Button就是创建房间
同样再复制一个Button重命名为JoinBtn，这个Button是用来加入房间

注意，此时我们想要在Button的Text中输入中文是不可行的，因为TextMeshPro的默认字体是不支持中文的，因此，我们需要一个中文字符集

①https://www.iconfont.cn中点击字体库下载阿里妈妈字体包
②下载完毕之后选中后缀为：.ttf的字体文件拖拽进入Unity
③在Unity中，右键该字体文件，Create -> TextMeshPro -> Font Asset生成了一个TextMeshPro字体文件
④选中TextMeshPro字体文件将它的Atlas Width和Atlas Height字段属性设置为最大（一般是8192）后点击Apply即可使用

创建新的脚本StartCtrl.cs并且在场景中创建一个空节点把它挂载上去
StartCtrl.cs内容：

using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class StartCtrl : MonoBehaviour
{
    [SerializeField]
    private Transform _canvas;

    private TMP_InputField _ip;
    private void Start()
    {
        Button createBtn = _canvas.Find("CreateBtn").GetComponent<Button>();
        Button joinBtn = _canvas.Find("JoinBtn").GetComponent<Button>();
        createBtn.onClick.AddListener(OnCreateClick);
        joinBtn.onClick.AddListener(OnJoinClick);
        _ip = _canvas.Find("IP").GetComponent<TMP_InputField>();//获得输入框中输入的IP
    }
    /// <summary>
    /// 创建房间当主机
    /// </summary>
    private void OnCreateClick()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;//强制转换获得UnityTransport
        //UnityTransport中可以配置IP地址和端口号
        transport.SetConnectionData(_ip.text, 7777);//配置IPV4地址(也就是用户输入的IP地址)和端口号(端口号通常为4位数且可以随意填写)
        NetworkManager.Singleton.StartHost();//这样就可以当主机了
        GameManager.Instance.LoadScene("Lobby");//通过网络连接载入到新场景,使用本地函数载入场景的话,客户端无法跟随主机的场景
    }
    /// <summary>
    /// 加入房间当客户端
    /// </summary>
    private void OnJoinClick()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;//强制转换获得UnityTransport
        //UnityTransport中可以配置IP地址和端口号
        transport.SetConnectionData(_ip.text, 7777);//配置IPV4地址(也就是用户输入的IP地址)和端口号(端口号通常为4位数且可以随意填写)
        NetworkManager.Singleton.StartClient();//这样就可以当客户端
        //因为该函数是以客户端的身份加入主机的房间,而不是自己创建一个房间,因此主机在哪个场景,客户端加入后就在哪个场景
    }
}

在CanvasUI中创建一个InputField在这里可以输入IP，默认输入：127.0.0.1(本机地址)
当我们点击创建房间或者点击加入房间的时候我们都会默认的获得一下该输入框中的IP地址，然后进行创建或者加入
===================================================================================================================================================================

===================================================================================================================================================================
3.跳转场景

修改我们之前的GameManager.cs脚本
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour//继承NetworkBehaviour是为了共享一些变量，并且需要用到新的切换场景的方法
{
    public static GameManager Instance;//将GameManager设置为单例模式
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);//将此游戏对象在场景切换时保留,当前的游戏对象即便它所在的场景被销毁,但是它会被自动传入到系统默认生成的新场景中而不会被销毁
        SceneManager.LoadScene(1);//载入Start场景
    }
    /// <summary>
    /// 公开一个玩家从网络上载入场景的方法
    /// </summary>
    public void LoadScene(string sceneName)
    {
        //这里需要客户端与主机同步加载同样的场景,所以需要继承NetworkBehaviour类来实现网络同步
        //这个是网络上来加载场景,它执行之后,其他玩家加入房间或者连接到服务器的时候会直接自动进入到它加载的场景里面来
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);//它会保留上一个场景中通过网络生成的物体
    }
}

总而言之NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);是客户端或者主机通过网络连接来载入场景的重要方法，如果不使用该方法而使用Unity本地的载入场景方法，那么很可能客户端与主机之间无法同步载入场景而导致客户端只在本地进行单机操作
===================================================================================================================================================================

===================================================================================================================================================================
4.搭建游戏大厅界面

进行UI界面构建
然后创建一个新的脚本LobbyCtrl.cs并创建空节点挂载LobbyCtrl脚本
LobbyCtrl.cs内容：

更改LobbyCtrl.cs脚本内容：

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCtrl : NetworkBehaviour//从现在开始我们进入了网络状态,所以我们要继承NetworkBehaviour
{
    [SerializeField]
    private Transform _canvas;
    private Transform _content;
    private GameObject _originCell;
    private Button _startBtn;
    private Toggle _ready;
    private List<PlayerListCell> _cellList;
    private void OnStartClick()
    {

    }
    private void OnReadyToggle(bool arg0)
    {

    }
    /// <summary>
    /// 添加玩家进入房间
    /// </summary>
    /// <param name="playerID">玩家ID</param>
    /// <param name="isReady">是否准备</param>
    public void AddPlayer(ulong playerID, bool isReady)
    {
        GameObject clone = Instantiate(_originCell);
        clone.transform.SetParent(_content, false);
        PlayerListCell cell = clone.GetComponent<PlayerListCell>();
        _cellList.Add(cell);
        cell.Initial(playerID, isReady);
        clone.SetActive(true);
    }
    /// <summary>
    /// 当玩家从网络上生成出来(克隆)时,这是一个生命周期函数,类似于Start不过是网络版
    /// </summary>
    public override void OnNetworkSpawn()
    {
        //这里需要不同的客户端同步房间里的玩家信息状态,因此需要继承NetworkBehaviour类来实现网络同步
        _content = _canvas.Find("List/Viewport/Content");
        _originCell = _content.Find("Cell").gameObject;
        _startBtn = _canvas.Find("StartBtn").GetComponent<Button>();
        _ready = _canvas.Find("Ready").GetComponent<Toggle>();
        _startBtn.onClick.AddListener(OnStartClick);
        _ready.onValueChanged.AddListener(OnReadyToggle);
        _cellList = new List<PlayerListCell>();
        //在创建网络主机后先把本地的用户传进来
        AddPlayer(NetworkManager.LocalClientId, false);//NetworkManager.LocalClientId是本地客户端自带的ID，先把自己添加到网络房间中
        base.OnNetworkSpawn();
    }
}

因为当玩家从网络上生成出来时，我们需要保存玩家的ID和信息，所以我们再创建一个脚本PlayerListCell.cs并把它挂载到Cell对象身上

PlayerListCell.cs内容：

using TMPro;
using UnityEngine;
//用于存储玩家的ID并且观察玩家有没有准备并且获得Cell之下的组件
public class PlayerListCell : MonoBehaviour
{
    private TMP_Text _name;
    private TMP_Text _ready;
    /// <summary>
    /// 初始化时存储玩家ID和是否准备信息并且获取它的组件
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="isReady"></param>
    public void Initial(ulong playerID, bool isReady)
    {
        _name = transform.Find("Name").GetComponent<TMP_Text>();
        _ready = transform.Find("Ready").GetComponent<TMP_Text>();
        _name.text = "玩家" + playerID;
        _ready.text = isReady ? "准备" : "未准备";
    }
}

===================================================================================================================================================================
重点注意：

当你使用Unity中的Netcode for Gameobjects插件时，如果你想要编写处理网络同步的脚本，那么你就需要继承NetworkBehaviour类。
这个类允许你在脚本中使用Unity的网络功能，以便在多个客户端之间同步对象的状态和行为。当你的脚本需要在网络游戏中控制对象的行为，并且需要在多个客户端之间同步时，就需要继承NetworkBehaviour类。
这样可以确保你的脚本能够正确地在网络游戏中运行和同步。
并且继承了NetworkBehaviour类的脚本一定要添加Network Object组件
===================================================================================================================================================================
6.添加网络上其他玩家

首先继续编写LobbyCrtl.cs的内容：

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
//封装一个结构体来定义存储用户信息便于扩展
public struct PlayerInfo : INetworkSerializable//网络序列化接口,为了让这个结构体或类能够在网络流中传递,需要使用该接口
{
    public ulong id;//玩家ID信息
    public bool isready;//玩家是否准备信息
    //这个函数中需要填写要在网络流中序列化的字段信息
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);//结构体必须在字段前添加ref声明表示按引用传递,类则不用,因为类直接传递的引用
        serializer.SerializeValue(ref isready);
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
    private List<PlayerListCell> _cellList;
    private Dictionary<ulong, PlayerInfo> _allPlayerInfos;
    private void OnStartClick()
    {

    }
    private void OnReadyToggle(bool arg0)
    {

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
        _cellList.Add(cell);
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
        _startBtn.onClick.AddListener(OnStartClick);
        _ready.onValueChanged.AddListener(OnReadyToggle);
        _cellList = new List<PlayerListCell>();
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;//NetworkManager.LocalClientId是本地客户端自带的ID,先把自己添加到网络房间中
        playerInfo.isready = false;
        //在创建网络主机后先把本地的用户传进来也就是自己
        AddPlayer(playerInfo);
        base.OnNetworkSpawn();
    }
    /// <summary>
    /// 当新的客户端连接的回调函数
    /// </summary>
    /// <param name="playerid"></param>
    private void OnClientConnect(ulong playerid)
    {
        //当新的客户端连接时,实例化新的玩家信息并抓取他的id信息
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = playerid;
        playerInfo.isready = false;
        //然后将该玩家加入到房间中
        AddPlayer(playerInfo);
        UpdateAllPlayerInfos();
    }
    /// <summary>
    /// 更新房间中所有玩家信息
    /// </summary>
    private void UpdateAllPlayerInfos()//在服务端的时候会调用这个方法
    {
        //通知其他所有的客户端,把它们没有的Player信息添加进去,让每一个客户端都遍历一遍所有玩家信息
        foreach (var item in _allPlayerInfos)
        {
            UpdatePlayerInfoClientRpc(item.Value);
        }
    }
    //此标签表示在客户端的LobbyCtrl会调用此段函数
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
        }
    }
}

重点注意：

因为PlayerInfo结构体需要在不同类以及主机，客户端和服务端之间进行引用或者值传递，简而言之，一个类或者结构体中的值或引用需要在网络通信流中互相传递信息，就需要接入INetworkSerializable接口，来实现网络序列化和反序列化，来保证信息传递的
高效，安全，有效。总而言之是在网络流中传递需要的字段信息来对它们进行序列化和反序列化

INetworkSerializable接口的实现可能是为了使PlayerInfo结构能够进行序列化和反序列化以进行网络通信。通过实现INetworkSerializable接口，PlayerInfo结构可以定义自己的序列化和反序列化方法，从而使其能够高效地在网络上传输。实现INetworkSerializable接口
的目的是为了为PlayerInfo结构提供一种标准化的方式，以便为网络传输做好准备。它允许该结构定义其数据的序列化和反序列化方式，确保在接收端能够准确地重建数据。这种方法通常用于网络编程，以确保自定义数据类型能够在网络上传输时被正确处理，并有助于在
通信过程中保持一致性和可靠性。
===================================================================================================================================================================

===================================================================================================================================================================
7.概念和框架初步讲解

NGO(NetCode for GameObjects)中的三种角色：
①Host：主机，既是服务器又是客户端，建立服务端，并且也会在服务端的位置建立一个客户端
②Server：服务端，建立服务端
③Client：客户端，建立客户端

NGO中的几个常用组件：
①NetworkManager：网络管理者
②NetworkObject：网络物体表示
③NetworkTransform：用于同步更新Transform
④NetworkAnimator：用于同步更新Animator

NGO中的数据同步:
①ServerRpc：客户端调用，服务器执行(服务器只有一个)
②ClientRpc：服务器调用，所有！客户端执行
ServerRpc：这是一个在客户端上调用的函数，但实际上在服务器上执行。举个例子，假设你正在制作一个多人游戏，玩家可以通过点击屏幕来移动他们的角色。在这种情况下，你可能会在客户端上有一个函数来处理点击事件，然后通过ServerRpc将这个动
	   作发送到服务器，服务器再将这个动作广播给所有的客户端，以确保所有的玩家都看到这个角色的移动。
ClientRpc：这是一个在服务器上调用的函数，但实际上在所有连接的客户端上执行。继续上面的例子，当服务器接收到一个玩家的移动动作后，它需要将这个动作广播给所有的客户端，以便他们可以更新他们的游戏状态。这就是通过调用ClientRpc来实现的。
举个例子：

public class PlayerController : NetworkBehaviour
{
    // ServerRpc，客户端调用，服务器执行
    [ServerRpc(RequireOwnership = false)]
    public void MovePlayerServerRpc(Vector3 newPosition, ServerRpcParams serverRpcParams = default)
    {
        // 验证移动是否合法，例如检查新位置是否在地图范围内等

        // 通过ClientRpc同步给所有客户端
        MovePlayerClientRpc(newPosition);
    }
    // ClientRpc，服务器调用，所有的！客户端执行
    [ClientRpc]
    public void MovePlayerClientRpc(Vector3 newPosition, ClientRpcParams clientRpcParams = default)
    {
        // 更新角色位置
        transform.position = newPosition;
    }
}

NGO中的数据同步如何使用？
①使用方法时，必须要在方法上面加上[ClientRpc]或者[ServerRpc]的Attribute
②你的方法名，后缀必须带ClientRpc或者ServerRpc
③调用这个方法的类，必须继承于NetworkBehaviour，挂载的物体必须有NetworkObject
④传参只能传值类型

NGO中的序列化Serialization
①序列化是为了传输数据
②NGO针对C#和Unity的基本类型，已经准备了内置的序列化
③针对C#，包括：bool,char,sbyte,byte,short,ushort,int,uint,long,ulong,float,double,and string
④针对Unity，包括：Color,Color32,Vector2,Vector3,Vector4,Quaternion.Ray,Ray2D
⑤另外，针对Enum也会自动序列化
以上类型在NGO中会自动序列化，我们不需要再手动序列化了

当你想序列化自定义结构体时，就需要用到自定义接口：INetworkSerializable
①你可以声明结构体继承于此接口，方便整体传输数据

注意：在C#中，int代表整数，那么uint就代表非负整数，long和ulong，short和ushort同理。
===================================================================================================================================================================

===================================================================================================================================================================
8.实现数据通信，更新准备状态

修改LobbyCtrl.cs内容：

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
//封装一个结构体来定义存储用户信息便于扩展
public struct PlayerInfo : INetworkSerializable//网络序列化接口,为了让这个结构体或类能够在网络流中传递,需要使用该接口
{
    public ulong id;//玩家ID信息
    public bool isready;//玩家是否准备信息
    //这个函数中需要填写要在网络流中序列化的字段信息
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);//结构体必须在字段前添加ref声明表示按引用传递,类则不用,因为类直接传递的引用
        serializer.SerializeValue(ref isready);
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
    private Dictionary<ulong, PlayerListCell> _cellDictionary;
    private Dictionary<ulong, PlayerInfo> _allPlayerInfos;
    private void OnStartClick()
    {

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
        _cellDictionary[player.id].SetReady(player.isready);
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
        _startBtn.onClick.AddListener(OnStartClick);
        _ready.onValueChanged.AddListener(OnReadyToggle);
        _cellDictionary = new Dictionary<ulong, PlayerListCell>();
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;//NetworkManager.LocalClientId是本地客户端自带的ID,先把自己添加到网络房间中
        playerInfo.isready = false;
        //在创建网络主机后先把本地的用户传进来也就是自己
        AddPlayer(playerInfo);
        base.OnNetworkSpawn();
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
        playerInfo.isready = false;
        //然后将该玩家加入到房间中
        AddPlayer(playerInfo);
        UpdateAllPlayerInfos();
    }
    /// <summary>
    /// 更新房间中所有玩家信息
    /// </summary>
    private void UpdateAllPlayerInfos()//在服务端的时候会调用这个方法
    {
        //通知其他所有的客户端,把它们没有的Player信息添加进去,让每一个客户端都遍历一遍所有玩家信息
        foreach (var item in _allPlayerInfos)
        {
            UpdatePlayerInfoClientRpc(item.Value);//此时,向所有的客户端发送Rpc让他们更新玩家信息列表
        }
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
            _cellDictionary[item.Key].SetReady(item.Value.isready);//更改Cell格子的文字显示
        }
    }
}
同步数据更新信息大致流程：
①修改本地数据
②如果用户是服务端则可以直接调用ClientRpc来通知所有客户端更新信息
③如果用户是客户端想要通知其他客户端同步更新数据，则先要调用ServerRpc来通知服务器，把修改的数据内容在服务端ServerRpc执行中进行更新数据，然后让服务端来执行ClientRpc来通知所有客户端执行调用的函数内容
注意：客户端修改数据想要让所有其他客户端同步数据，则需要先在ServerRpc中把修改的数据同步给服务端，服务端才能得到修改后的数据再分发给其他客户端进行数据同步

[ServerRpc(RequireOwnership = false)]的含义：
在Unity的Netcode for GameObjects (NGO)中，RequireOwnership参数是一个布尔值，用于指定是否只有拥有该GameObject的客户端才能调用此ServerRpc。
如果RequireOwnership设置为true（默认值），那么只有拥有该GameObject的客户端才能调用此ServerRpc。如果一个没有拥有权的客户端尝试调用此ServerRpc，那么将会收到一个错误。
如果RequireOwnership设置为false，那么任何客户端都可以调用此ServerRpc，不论他们是否拥有该GameObject。
在一些情况下，你可能希望允许任何客户端都可以调用某个ServerRpc（例如，一个全局的聊天功能），那么你可以设置RequireOwnership为false。
而在其他情况下，你可能只希望拥有某个GameObject的客户端才能控制它（例如，一个玩家控制的角色），那么你可以设置RequireOwnership为true。
===================================================================================================================================================================

===================================================================================================================================================================
9.实现数据通信，选择男女

先为玩家信息面板，也就是Cell添加性别信息字段
然后修改PlayerListCell.cs脚本内容：

using TMPro;
using UnityEngine;
//用于存储玩家的ID并且观察玩家有没有准备并且获得Cell之下的组件
public class PlayerListCell : MonoBehaviour
{
    private TMP_Text _name;
    private TMP_Text _ready;
    private TMP_Text _gender;
    public PlayerInfo PlayerInfo { get; private set; }
    /// <summary>
    /// 初始化时存储玩家ID和是否准备信息并且获取它的组件
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="isReady"></param>
    public void Initial(PlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
        _name = transform.Find("Name").GetComponent<TMP_Text>();
        _ready = transform.Find("Ready").GetComponent<TMP_Text>();
        _gender = transform.Find("Gender").GetComponent<TMP_Text>();
        _name.text = "玩家" + playerInfo.id;
        _ready.text = playerInfo.isready ? "准备" : "未准备";
        _gender.text = playerInfo.gender == 0 ? "男" : "女";
    }
    internal void SetReady(bool arg0)
    {
        _ready.text = arg0 ? "准备" : "未准备";
    }
    public void SetGender(int gender)
    {
        _gender.text = gender == 0 ? "男" : "女";
    }
}
新增了gender性别属性栏，同理LobbyCtrl.cs脚本中也要对应的新增gender性别信息来相应的修改脚本

新建一个Camera来输出一个动态的Render Texture放置于RawImage上来开发一个实时渲染的动态UI，来展示选择的男性或者女性的人物模型

新建一个脚本BodyCtrl.cs并挂载到摄像机上来控制人物选择等功能
BodyCtrl.cs内容：

using UnityEngine;

public class BodyCtrl : MonoBehaviour
{
    public static BodyCtrl Instance;
    private void Start()
    {
        Instance = this;
    }
    public void SwitchGender(int gender)
    {
        transform.GetChild(gender).gameObject.SetActive(true);
        transform.GetChild(1-gender).gameObject.SetActive(false);
    }
}

更新后的LobbyCtrl.cs内容：

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
//封装一个结构体来定义存储用户信息便于扩展
public struct PlayerInfo : INetworkSerializable//网络序列化接口,为了让这个结构体或类能够在网络流中传递,需要使用该接口
{
    public ulong id;//玩家ID信息
    public bool isready;//玩家是否准备信息
    public int gender;//0代表男,1代表女
    //这个函数中需要填写要在网络流中序列化的字段信息
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);//结构体必须在字段前添加ref声明表示按引用传递,类则不用,因为类直接传递的引用
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
    private Dictionary<ulong, PlayerListCell> _cellDictionary;
    private Dictionary<ulong, PlayerInfo> _allPlayerInfos;
    private void OnStartClick()
    {

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
        _startBtn.onClick.AddListener(OnStartClick);
        _ready.onValueChanged.AddListener(OnReadyToggle);
        _cellDictionary = new Dictionary<ulong, PlayerListCell>();
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;//NetworkManager.LocalClientId是本地客户端自带的ID,先把自己添加到网络房间中
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
        playerInfo.isready = false;
        //然后将该玩家加入到房间中
        AddPlayer(playerInfo);
        UpdateAllPlayerInfos();
    }
    /// <summary>
    /// 更新房间中所有玩家信息
    /// </summary>
    private void UpdateAllPlayerInfos()//在服务端的时候会调用这个方法
    {
        //通知其他所有的客户端,把它们没有的Player信息添加进去,让每一个客户端都遍历一遍所有玩家信息
        foreach (var item in _allPlayerInfos)
        {
            UpdatePlayerInfoClientRpc(item.Value);//此时,向所有的客户端发送Rpc让他们更新玩家信息列表
        }
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

注意：
①将无数个Toggle的父物体添加一个组件ToggleGroup的话，再将ToggleGroup拖入无数的Toggle的对应属性栏中，那么被ToggleGroup所控制的Toggle将会变成单选模式，也就是每次只能选中一个Toggle

②Unity中，Image和Raw Image的区别：
    Image：
        Image组件用于显示普通的2D图片，比如PNG、JPG等格式的图片。
        适合用于显示普通的UI元素和背景等。
    Raw Image：
        Raw Image组件用于显示未经压缩的图片数据，比如直接从相机中捕捉到的图像数据或者通过代码动态加载的纹理数据。
        适合用于显示实时生成的图像或者需要动态更新的纹理数据。

③如可快速实现一个摄像机动态渲染图像UI至画面中：
首先创建一个RawImage组件并记录该组件的宽度大小
然后在Project中创建一个Render Texture文件，并且将文件大小宽度设置的和RawImage的大小宽度一致
然后在Hierarchy面板中创建一个摄像机，将Render Texture文件拖入摄像机的Output属性栏中
最后再将Render Texture文件拖入RawImage中
这样，RawImage中就会实时显示摄像机Camera中捕捉到的画面了！
===================================================================================================================================================================

===================================================================================================================================================================
10.自定义名称

在UI面板新建InputField输入框，让用户能够输入名称字段
修改LobbyCtrl.cs内容：

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
    private void OnStartClick()
    {

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
    private void UpdateAllPlayerInfos()//在服务端的时候会调用这个方法
    {
        //通知其他所有的客户端,把它们没有的Player信息添加进去,让每一个客户端都遍历一遍所有玩家信息
        foreach (var item in _allPlayerInfos)
        {
            UpdatePlayerInfoClientRpc(item.Value);//此时,向所有的客户端发送Rpc让他们更新玩家信息列表
        }
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

主要是添加name字段，修改本地name数据后再传入网络流修改所有客户端数据
===================================================================================================================================================================

===================================================================================================================================================================
11.正式进入游戏

我们需要实现的是，当所有玩家准备之后，可以进入游戏
更新一下LobbyCtrl.cs内容：

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

注意：
①OnClientConnect函数是当一个客户端连接进入服务端，服务端所产生的回调事件函数，它是只是在服务端会执行其中的内容，因此UpdateAllPlayerInfos函数也是只是服务端会执行的函数内容，因此UpdateAllPlayerInfos函数所产生的数据变化不会映射影响到客户端
②NetworkManager.SceneManager.LoadScene和SceneManager.LoadScene的区别：

SceneManager.LoadScene():
1.Unity 内置方法，用于单机模式下加载场景
2.适用于普通的单人游戏或不涉及网络同步的场景切换
3.不包含网络同步的功能

NetworkManager.SceneManager.LoadScene():
1.Netcode for GameObjects 插件中的方法，专门用于网络游戏场景加载
2.支持在网络游戏中进行场景加载，并与网络同步功能结合使用
3.适用于多人游戏场景中，希望实现多个玩家同时切换到同一个场景，并确保在网络环境下场景同步的情况

只有服务端可以执行NetworkManager.SceneManager.LoadScene()函数，只有服务端可以开启一个场景事件！
===================================================================================================================================================================

===================================================================================================================================================================
12.搭建聊天窗口

在Game场景创建一个玩家出生点PlayerSpawn

创建新的脚本GameCtrl.cs并且将该脚本挂载至PlayerSpawn上

创建聊天窗口UI界面

编写GameCtrl.cs内容：

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameCtrl : MonoBehaviour
{
    [SerializeField]
    private Transform _canvas;
    private TMP_InputField _input;
    private RectTransform _content;
    private GameObject _dialogCell;
    private void Start()
    {
        _input = _canvas.Find("Dialog/Input").GetComponent<TMP_InputField>();
        _content = _canvas.Find("Dialog/DialogPanel/Viewport/Content") as RectTransform;
        _dialogCell = _canvas.Find("Cell").gameObject;
        Button sendBtn = _canvas.Find("Dialog/SendBtn").GetComponent<Button>();
        sendBtn.onClick.AddListener(OnSendClick);
    }
    private void OnSendClick()
    {
        if (string.IsNullOrEmpty(_input.text))
        {
            return;
        }
        AddDialogCell("", "");
    }
    private void AddDialogCell(string playerName, string content)
    {
        GameObject clone = Instantiate(_dialogCell);
        clone.transform.SetParent(_content, false);
        clone.AddComponent<DialogCell>().Initial(playerName, content);
        clone.SetActive(true);
    }
}

创建一个新的脚本DialogCell.cs用来管理我们的Cell
DialogCell.cs内容：

using TMPro;
using UnityEngine;

public class DialogCell : MonoBehaviour
{
    public void Initial(string playerName, string content)
    {
        TMP_Text nameText = transform.Find("Name").GetComponent<TMP_Text>();
        TMP_Text contentText = transform.Find("Content").GetComponent<TMP_Text>();
        nameText.text = playerName;
        contentText.text = content;
    }
}

===================================================================================================================================================================

===================================================================================================================================================================
13.实现本地玩家发送消息

我们全局需要拿到所有PlayerInfo信息，所以我们
修改GameManager.cs脚本内容：

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour//继承NetworkBehaviour是为了共享一些变量，并且需要用到新的切换场景的方法
{
    public static GameManager Instance;//将GameManager设置为单例模式
    public Dictionary<ulong, PlayerInfo> AllPlayerInfos { get; private set; }
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);//将此游戏对象在场景切换时保留,当前的游戏对象即便它所在的场景被销毁,但是它会被自动传入到系统默认生成的新场景中而不会被销毁
        SceneManager.LoadScene(1);//载入Start场景
        AllPlayerInfos = new Dictionary<ulong, PlayerInfo>();
    }
    public void StartGame(Dictionary<ulong, PlayerInfo> playerInfos)
    {
        AllPlayerInfos = playerInfos;
        UpdateAllPlayerInfos();
    }
    private void UpdateAllPlayerInfos()
    {
        foreach (var playerInfo in AllPlayerInfos)
        {
            UpdatePlayerInfoClientRpc(playerInfo.Value);
        }
    }
    [ClientRpc]
    private void UpdatePlayerInfoClientRpc(PlayerInfo playerInfo)
    {
        if (!IsServer)
        {
            if (AllPlayerInfos.ContainsKey(playerInfo.id))//先判断字典中是否有玩家ID
            {
                AllPlayerInfos[playerInfo.id] = playerInfo;//有就更新一下客户端玩家信息
            }
            else
            {
                AllPlayerInfos.Add(playerInfo.id, playerInfo);
            }
        }
    }
    /// <summary>
    /// 公开一个玩家从网络上载入场景的方法
    /// </summary>
    public void LoadScene(string sceneName)
    {
        //这里需要客户端与主机同步加载同样的场景,所以需要继承NetworkBehaviour类来实现网络同步
        //这个是网络上来加载场景,它执行之后,其他玩家加入房间或者连接到服务器的时候会直接自动进入到它加载的场景里面来
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);//它会保留上一个场景中通过网络生成的物体
    }
}


修改GameCtrl.cs内容：

using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameCtrl : MonoBehaviour
{
    [SerializeField]
    private Transform _canvas;
    private TMP_InputField _input;
    private RectTransform _content;
    private GameObject _dialogCell;
    private void Start()
    {
        _input = _canvas.Find("Dialog/Input").GetComponent<TMP_InputField>();
        _content = _canvas.Find("Dialog/DialogPanel/Viewport/Content") as RectTransform;
        _dialogCell = _content.Find("Cell").gameObject;
        Button sendBtn = _canvas.Find("Dialog/SendBtn").GetComponent<Button>();
        sendBtn.onClick.AddListener(OnSendClick);
    }
    private void OnSendClick()
    {
        if (string.IsNullOrEmpty(_input.text))
        {
            return;
        }
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[NetworkManager.Singleton.LocalClientId];
        AddDialogCell(playerInfo.name, _input.text);
    }
    private void AddDialogCell(string playerName, string content)
    {
        GameObject clone = Instantiate(_dialogCell);
        clone.transform.SetParent(_content, false);
        clone.AddComponent<DialogCell>().Initial(playerName, content);
        clone.SetActive(true);
    }
}

===================================================================================================================================================================

===================================================================================================================================================================
14.实现玩家互传消息

注意：在Unity网络生命周期中，OnNetworkSpawn中的内容是先于Start执行的，因此，原先一些由Start处理的初始化行为应该转为由OnNetworkSpawn去代替处理

修改GameCtrl.cs内容：

using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameCtrl : NetworkBehaviour
{
    [SerializeField]
    private Transform _canvas;
    private TMP_InputField _input;
    private RectTransform _content;
    private GameObject _dialogCell;
    public override void OnNetworkSpawn()
    {
        _input = _canvas.Find("Dialog/Input").GetComponent<TMP_InputField>();
        _content = _canvas.Find("Dialog/DialogPanel/Viewport/Content") as RectTransform;
        _dialogCell = _content.Find("Cell").gameObject;
        Button sendBtn = _canvas.Find("Dialog/SendBtn").GetComponent<Button>();
        sendBtn.onClick.AddListener(OnSendClick);
        base.OnNetworkSpawn();
    }
    private void OnSendClick()
    {
        if (string.IsNullOrEmpty(_input.text))
        {
            return;
        }
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[NetworkManager.Singleton.LocalClientId];
        AddDialogCell(playerInfo.name, _input.text);
        if (IsServer)
        {
            SendMsgToOthersClientRpc(playerInfo, _input.text);
        }
        else
        {
            SendMsgToOthersServerRpc(playerInfo, _input.text);
        }
    }
    [ClientRpc]
    private void SendMsgToOthersClientRpc(PlayerInfo playerInfo, string content)
    {
        if (!IsServer && playerInfo.id != NetworkManager.LocalClientId)//避免发送给自己,防止出现两次我们说过的话
        {
            AddDialogCell(playerInfo.name, content);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SendMsgToOthersServerRpc(PlayerInfo playerInfo, string content)
    {
        AddDialogCell(playerInfo.name, content);//先将传入的新的数据参数给服务端
        //此时,服务器的数据已经更新,再通知其他所有客户端更新服务器的最新数据
        SendMsgToOthersClientRpc(playerInfo, content);
    }
    private void AddDialogCell(string playerName, string content)
    {
        GameObject clone = Instantiate(_dialogCell);
        clone.transform.SetParent(_content, false);
        clone.AddComponent<DialogCell>().Initial(playerName, content);
        clone.SetActive(true);
    }
}

===================================================================================================================================================================

===================================================================================================================================================================
15.绑定角色动画


===================================================================================================================================================================

===================================================================================================================================================================
16.绑定角色相机

导入Cinemachine摄像机插件，并且导入Cinemachine插件的Samples示例文件，该示例文件中有大量官方配置好的摄像机移动视角样例，一定有你项目需要的！

找到Samples示例文件中的第三人称示例场景，将场景中的示例摄像机(Normal)拖到自己的游戏场景中去，我们直接使用官方设定好的第三人称摄像机，选择我们就缺一个摄像机的追随目标Target
通过修改GameCtrl.cs内容来设置摄像机目标：

using Cinemachine;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameCtrl : NetworkBehaviour
{
    public static GameCtrl Instance;
    [SerializeField]
    private Transform _canvas;
    [SerializeField]
    private CinemachineVirtualCamera _cameraCtrl;
    private TMP_InputField _input;
    private RectTransform _content;
    private GameObject _dialogCell;
    public override void OnNetworkSpawn()
    {
        Instance = this;
        _input = _canvas.Find("Dialog/Input").GetComponent<TMP_InputField>();
        _content = _canvas.Find("Dialog/DialogPanel/Viewport/Content") as RectTransform;
        _dialogCell = _content.Find("Cell").gameObject;
        Button sendBtn = _canvas.Find("Dialog/SendBtn").GetComponent<Button>();
        sendBtn.onClick.AddListener(OnSendClick);
        base.OnNetworkSpawn();
    }
    private void OnSendClick()
    {
        if (string.IsNullOrEmpty(_input.text))
        {
            return;
        }
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[NetworkManager.Singleton.LocalClientId];
        AddDialogCell(playerInfo.name, _input.text);
        if (IsServer)
        {
            SendMsgToOthersClientRpc(playerInfo, _input.text);
        }
        else
        {
            SendMsgToOthersServerRpc(playerInfo, _input.text);
        }
    }
    [ClientRpc]
    private void SendMsgToOthersClientRpc(PlayerInfo playerInfo, string content)
    {
        if (!IsServer && playerInfo.id != NetworkManager.LocalClientId)//避免发送给自己,防止出现两次我们说过的话
        {
            AddDialogCell(playerInfo.name, content);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SendMsgToOthersServerRpc(PlayerInfo playerInfo, string content)
    {
        AddDialogCell(playerInfo.name, content);//先将传入的新的数据参数给服务端
        //此时,服务器的数据已经更新,再通知其他所有客户端更新服务器的最新数据
        SendMsgToOthersClientRpc(playerInfo, content);
    }
    private void AddDialogCell(string playerName, string content)
    {
        GameObject clone = Instantiate(_dialogCell);
        clone.transform.SetParent(_content, false);
        clone.AddComponent<DialogCell>().Initial(playerName, content);
        clone.SetActive(true);
    }
    public void SetFollowTarget(Transform target)
    {
        _cameraCtrl.Follow = target;
    }
}

设置一个公共函数来设定摄像机跟随的玩家位置，当玩家进入Game场景直接调用该公共函数设定将自己位置传参给target

因此，我们现在还需要一个脚本PlayerInit.cs来设置玩家进入游戏的初始化逻辑，将脚本挂载到游戏角色预制体上
PlayerInit.cs内容：

using Unity.Netcode;
using UnityEngine;

public class PlayerInit : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnStartGame.AddListener(OnStartGame);
        base.OnNetworkSpawn();
    }
    private void OnStartGame()
    {
        //这里是服务端控制的所有客户端同时开始进入游戏的函数,因此应该使用OwnerClientId来
        //确保每一个客户端通过各自自己的唯一ID标识获得的是自己的各自信息
        //如果使用LocalClientId则这里同时所有客户端获得的信息都是服务端的信息导致所有客户端都长的和服务端一样
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[OwnerClientId];//OwnerClientId本人自己的ID，而不是本地客户端ID
        Transform body = transform.GetChild(playerInfo.gender);
        body.gameObject.SetActive(true);
        //因为多人游戏中有多个客户端,添加IsLocalPlayer条件是为了每一个客户端只执行自己客户端的代码,而不去调用其他客户端的代码
        if (IsLocalPlayer)//IsLocalPlayer判断是否是本地客户端,只有本地客户端执行此代码
        {
            GameCtrl.Instance.SetFollowTarget(body);//将摄像机对准玩家
        }
    }
}

另外，我们还需修改一下GameManager.cs内容，来设置一下游戏加载时的委托事件和使用NetworkBehaiver自带的场景加载完成的回调事件
GameManager.cs内容：

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour//继承NetworkBehaviour是为了共享一些变量，并且需要用到新的切换场景的方法
{
    public static GameManager Instance;//将GameManager设置为单例模式
    public UnityEvent OnStartGame;
    public Dictionary<ulong, PlayerInfo> AllPlayerInfos { get; private set; }
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);//将此游戏对象在场景切换时保留,当前的游戏对象即便它所在的场景被销毁,但是它会被自动传入到系统默认生成的新场景中而不会被销毁
        SceneManager.LoadScene(1);//载入Start场景
        AllPlayerInfos = new Dictionary<ulong, PlayerInfo>();
    }
    public override void OnNetworkSpawn()
    {
        NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventComplete;//网络流加载场景自带的所有客户端场景加载完成时回调事件
        base.OnNetworkSpawn();
    }
    private void OnLoadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "Game")
        {
            OnStartGame.Invoke();
        }
    }
    public void StartGame(Dictionary<ulong, PlayerInfo> playerInfos)
    {
        AllPlayerInfos = playerInfos;
        UpdateAllPlayerInfos();
    }
    private void UpdateAllPlayerInfos()
    {
        foreach (var playerInfo in AllPlayerInfos)
        {
            UpdatePlayerInfoClientRpc(playerInfo.Value);
        }
    }
    [ClientRpc]
    private void UpdatePlayerInfoClientRpc(PlayerInfo playerInfo)
    {
        if (!IsServer)
        {
            if (AllPlayerInfos.ContainsKey(playerInfo.id))//先判断字典中是否有玩家ID
            {
                AllPlayerInfos[playerInfo.id] = playerInfo;//有就更新一下客户端玩家信息
            }
            else
            {
                AllPlayerInfos.Add(playerInfo.id, playerInfo);
            }
        }
    }
    /// <summary>
    /// 公开一个玩家从网络上载入场景的方法
    /// </summary>
    public void LoadScene(string sceneName)
    {
        //这里需要客户端与主机同步加载同样的场景,所以需要继承NetworkBehaviour类来实现网络同步
        //这个是网络上来加载场景,它执行之后,其他玩家加入房间或者连接到服务器的时候会直接自动进入到它加载的场景里面来
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);//它会保留上一个场景中通过网络生成的物体
    }
}

注意：
①NetworkManager.SceneManager.OnLoadComplete是网络流加载场景自带的单个场景加载完成时就会产生一次的回调事件
    NetworkManager.SceneManager.OnLoadEventComplete是网络流加载场景自带的当所有客户端服务端的场景同时全部加载完成时才产生一次的回调事件，它还可以获得超时或者未超时的客户端的玩家ID

②LocalClientId和OwnerClientId的区别：
OwnerClientId属性： 这个属性用于标识拥有（owning）特定网络对象的客户端的唯一标识符。当你需要在服务器端或其他客户端上确定哪个客户端拥有特定的网络对象时，可以使用OwnerClientId属性
LocalClientId属性： 这个属性用于标识当前客户端的唯一标识符。当你需要在本地客户端上确定当前客户端的身份时，可以使用LocalClientId属性。
如果你需要与特定网络对象的所有者进行交互，你可以使用OwnerClientId属性。而如果你需要在本地客户端上执行一些特定的逻辑，你可以使用LocalClientId属性
在我们的PlayerInit类中：
        这里是服务端控制的所有客户端同时开始进入游戏的函数,因此应该使用OwnerClientId来确保每一个客户端通过各自自己的唯一ID标识获得的是自己的各自信息，如果使用LocalClientId则这里同时所有客户端获得的信息都是服务端的信息导致所有客户端的玩家都长的和服务端的玩家一样！

③IsLocalPlayer：
因为多人游戏中有多个客户端,添加IsLocalPlayer条件是为了每一个客户端只执行自己客户端的代码,而不去调用其他客户端的代码，判断是否是本地客户端,只有本地客户端执行此代码
===================================================================================================================================================================

===================================================================================================================================================================
17.解决前两期遗留问题


===================================================================================================================================================================

===================================================================================================================================================================
18.设置玩家出生位置

修改GameCtrl.cs内容，来让玩家在一个范围内随机出生，因为人物的出生人物设置设定都是通过服务器分发的位置数据，本身就是同步的，因此不需要进行玩家位置信息的网络流同步

GameCtrl.cs新添加内容：

    public Vector3 GetSpawnPos()
    {
        Vector3 pos = new Vector3();
        Vector3 offset = transform.forward * Random.Range(-10.0f, 10.0f) + transform.right * Random.Range(-10.0f, 10.0f);//获得偏移值
        pos = transform.position + offset;
        return pos;
    }

===================================================================================================================================================================

===================================================================================================================================================================
19.框架及相关概念讲解

①四个场景：
1.初始化
2.开始
3.大厅
4.游戏

②五大管理类
1.GameManager
2.StartCtrl
3.LobbyCtrl
4.GameCtrl
5.NetworkManager

除了NetworkManager管理类是NGO自带的核心管理类，其余四个管理类是我们自定义的框架，基本对应以上四个场景

③跳转结构：初始化场景-->开始场景-->大厅场景-->游戏场景
大厅场景和游戏场景可以点击退出游戏函数跳转至开始场景，初始化场景除了进入游戏的一刻执行一次之后再也不会进入

详细解构：
Ⅰ.初始化场景：

GameManager：游戏管理者，DontDestroyOnLoad（不会因为场景销毁而被销毁会在新的场景留存），继承NetworkBehaviour，负责场景跳转
NetworkManager：网络管理者，DontDestroyOnLoad

Ⅱ.开始场景：

StartCtrl：开始场景管理者，负责UI，启动服务或连接服务等网络工作
重要函数方法：StartHost(创建主机)、StartServer(创建服务器)、StartClient(创建客户端)

重要知识点：IP地址
127.0.0.1=localhost，它就是我们的本机地址，都是回环地址，127.0.0.1是IP，localhost是一个域名（指向127.0.0.1），这种地址只能本机访问本机，外部无法访问
192.168开头的地址，属于路由器分配的地址，局域网地址
公网、外网、互联网地址（例如bilibili），大家都可以访问到
百度搜到的IP是动态IP，普通情况无法访问到家里的电脑或手机
*启动服务时，可以使用0.0.0.0，表明可以从外部访问的IP，使用这个IP地址时，服务器的IP地址就不用管了，客户端连接时，只要知道服务器的IP就一定能连接上！

Ⅲ.大厅场景：

LobbyCtrl：继承于NetworkBehaviour，负责UI，玩家信息存储和传输
网络流相关重要的一些函数以及属性：
数据同步：远程调用（RPC）：ServerRpc、ClientRpc
	同步变量（NetworkVariable）：NetworkVariable<T>（同步玩家位置信息时通常使用）、INetworkSerializable（通常大部分值类型和常用的引用类型NGO自动帮我们同步数据完成不需要过多修改，但是自定义的结构体值类型需要网络序列化否则数据无法同步成功）
标志位及属性：IsLocalPlayer（是否是本地玩家，当在一个场景中有我们自己和许多其他玩家的镜像时，这个条件很重要）、IsServer（是否是服务器）、IsClient（是否是客户端）
	       LocalClientId（本地玩家ID）、OwnerId（拥有者ID）、NetworkObjectId（组件ID，每一个组件都有一个这个ID）
重写方法：OnNetworkSpawn（克隆）、OnNetworkDespawn（销毁）
事件：OnClientConnectedCallBack（玩家连接时）、OnLoadEventCompleted（场景加载完成时）

Ⅳ.游戏场景：
GameCtrl：继承于NetworkBehaviour，负责UI，玩家信息存储和传输
PlayerInit：继承于NetworkBehaviour，负责玩家初始化，根据IsLocalPlayer决定是否启用玩家控制脚本
PlayerCtrl：继承于MonoBehaviour，负责玩家控制
对应数据同步方法：
聊天室（消息同步）---（远程调用）RPC
玩家同步（位置和动画）---（同步变量）NetworkVariable
敌人和NPC同步（服务端的物体同步）---（同步变量）NetworkVariable
===================================================================================================================================================================

===================================================================================================================================================================
20.同步玩家位置

现在我们需要为玩家添加控制移动脚本，由于Cinemachine插件除了给我们提供了许多控制摄像机视角的样例模板，还为我们提供了许多编写好的人物移动脚本，我们可以直接使用

因此我们选中我们的人物预制体并且给他们添加Cinemachine插件提供的人物移动脚本PlayerMove.cs然后添加刚体组件再加上CapsuleCollider来表示人物碰撞体

现在，我们需要自定义PlayerMove.cs脚本：

using Cinemachine.Utility;
using System;
using UnityEngine;
public class PlayerMove : MonoBehaviour
{
    public float Speed;
    public float VelocityDamping;
    public float JumpTime;

    public enum ForwardMode
    {
        Camera,
        Player,
        World
    };

    public ForwardMode InputForward;

    public bool RotatePlayer = true;

    public Action SpaceAction;
    public Action EnterAction;

    Vector3 m_currentVleocity;
    float m_currentJumpSpeed;
    float m_restY;

    Animator _selfAnim;
    private void Reset()
    {
        Speed = 5;
        InputForward = ForwardMode.Camera;
        RotatePlayer = true;
        VelocityDamping = 0.5f;
        m_currentVleocity = Vector3.zero;
        JumpTime = 1;
        m_currentJumpSpeed = 0;
    }

    private void OnEnable()
    {
        _selfAnim = GetComponent<Animator>();
        m_currentJumpSpeed = 0;
        m_restY = transform.position.y;
        SpaceAction -= Jump;
        SpaceAction += Jump;
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        Vector3 fwd;
        switch (InputForward)
        {
            case ForwardMode.Camera:
                fwd = Camera.main.transform.forward;
                break;
            case ForwardMode.Player:
                fwd = transform.forward;
                break;
            case ForwardMode.World:
            default:
                fwd = Vector3.forward;
                break;
        }

        fwd.y = 0;
        fwd = fwd.normalized;
        if (fwd.sqrMagnitude < 0.01f)
            return;

        Quaternion inputFrame = Quaternion.LookRotation(fwd, Vector3.up);
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        input = inputFrame * input;

        var dt = Time.deltaTime;
        var desiredVelocity = input * Speed;
        var deltaVel = desiredVelocity - m_currentVleocity;
        m_currentVleocity += Damper.Damp(deltaVel, VelocityDamping, dt);
        _selfAnim.SetFloat("NormalMoveSpeed", input.magnitude);
        transform.position += m_currentVleocity * dt;
        if (RotatePlayer && m_currentVleocity.sqrMagnitude > 0.01f)
        {
            var qA = transform.rotation;
            var qB = Quaternion.LookRotation(
                (InputForward == ForwardMode.Player && Vector3.Dot(fwd, m_currentVleocity) < 0)
                    ? -m_currentVleocity
                    : m_currentVleocity);
            transform.rotation = Quaternion.Slerp(qA, qB, Damper.Damp(1, VelocityDamping, dt));
        }

        // Process jump
        if (m_currentJumpSpeed != 0)
            m_currentJumpSpeed -= 10 * dt;
        var p = transform.position;
        p.y += m_currentJumpSpeed * dt;
        if (p.y < m_restY)
        {
            p.y = m_restY;
            m_currentJumpSpeed = 0;
        }

        transform.position = p;

        if (Input.GetKeyDown(KeyCode.Space) && SpaceAction != null)
            SpaceAction();
        if (Input.GetKeyDown(KeyCode.Return) && EnterAction != null)
            EnterAction();
#else
        InputSystemHelper.EnableBackendsWarningMessage();
#endif
    }

    public void Jump()
    {
        m_currentJumpSpeed += 10 * JumpTime * 0.5f;
    }
}

接下来我们需要编写同步脚本，创建PlayerSync.cs内容：

using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerSync : NetworkBehaviour
{
    //声明两个同步变量,需要同步玩家之间的位置Vector3和玩家之间的旋转方向Quaternion
    private NetworkVariable<Vector3> _syncPos = new NetworkVariable<Vector3>();//NetworkVariable就是存储在服务端中的数据
    private NetworkVariable<Quaternion> _syncRot = new NetworkVariable<Quaternion>();//NetworkVariable变量存储的数据本身就是在服务端上的数据,只能在服务端的身份可以对它进行写入操作,例如IsServer或者ServerRpc,读取NetworkVariable的数据本质上就是直接从服务端读取数据
    private Transform _syncTransform;
    private void Update()
    {
        //如果是本地玩家,你就不停的上传你的位置坐标;如果你是其他玩家客户端,你就不停的下载位置坐标
        if (IsLocalPlayer)
        {
            UploadTransform();
        }
    }
    //防止两边的帧不一样导致同步错误,因此下载其他客户端传入的位置数据一般是放置在FixedUpdate生命周期中
    private void FixedUpdate()
    {
        if (!IsLocalPlayer)//判断场景中的人物不是自己是其他客户端才进行从服务端下载数据操作
        {
            SyncTransform();
        }
    }
    public void SetTarget(int gender)
    {
        _syncTransform = transform.GetChild(gender);
    }
    private void SyncTransform()
    {
        _syncTransform.position = _syncPos.Value;
        _syncTransform.rotation = _syncRot.Value;//从服务端的NetworkVariable拿到数据即可
        //场景中的其他客户端也只需要从服务端不断更新下载你的位置信息不需要多余的操作
    }
    //上传位置坐标至服务端
    private void UploadTransform()
    {
        if (IsServer)//判断一下如果你是主机的话则直接本地赋值,不需要再上传至服务端了
        {
            _syncPos.Value = _syncTransform.position;
            _syncRot.Value = _syncTransform.rotation;//只能是在服务端的身份下对NetworkVariable进行写入操作
        }
        else//如果你是普通的客户端,则需要先将数据上传至服务端
        {
            UploadTransformServerRpc(_syncTransform.position, _syncTransform.rotation);
        }
    }
    [ServerRpc]
    private void UploadTransformServerRpc(Vector3 position, Quaternion rotation)
    {
        _syncPos.Value = position;//只能是在服务端的身份下对NetworkVariable进行写入操作
        _syncRot.Value = rotation;//本地数据上传至服务端的NetworkVariable变量中即可
        //因为你自己的坐标位置只能由自己控制,因此不需要执行下载坐标位置数据操作,不能让别的客户端来控制你的人物的位置,因此只需要上传自己的位置数据给服务端即可
    }
}

将脚本挂载到Player预制体根节点即可
位置同步数据需要利用NetworkVariable变量来做载体来在各个客户端和服务端之间进行数据传输
IsLocalPlayer：因为游玩儿多人网络游戏时，场景中有许多被控制的人物对象，该标签可以让你分辨哪一个是你自己控制的人物客户端，便于对自己进行逻辑操作

同时还需要更新一些人物初始化脚本函数PlayerInit.cs内容：

    private void OnStartGame()
    {
        //这里是服务端控制的所有客户端同时开始进入游戏的函数,因此应该使用OwnerClientId来
        //确保每一个客户端通过各自自己的唯一ID标识获得的是自己的各自信息
        //如果使用LocalClientId则这里同时所有客户端获得的信息都是服务端的信息导致所有客户端都长的和服务端一样
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[OwnerClientId];//OwnerClientId本人自己的ID，而不是本地客户端ID
        Transform body = transform.GetChild(playerInfo.gender);
        body.gameObject.SetActive(true);
        PlayerSync playerSync = GetComponent<PlayerSync>();
        playerSync.SetTarget(playerInfo.gender);
        playerSync.enabled = true;
        //因为多人游戏中有多个客户端,添加IsLocalPlayer条件是为了每一个客户端只执行自己客户端的代码,而不去调用其他客户端的代码
        if (IsLocalPlayer)//IsLocalPlayer判断是否是本地客户端,只有本地客户端执行此代码
        {
            GameCtrl.Instance.SetFollowTarget(body);//将摄像机对准玩家
            body.GetComponent<PlayerMove>().enabled = true;//只有当客户端知道自己是自己的时候才启用控制移动函数脚本
        }
        transform.position = GameCtrl.Instance.GetSpawnPos();
    }
===================================================================================================================================================================

===================================================================================================================================================================
21.同步玩家动画

为玩家预制体添加一个NGO自带的同步组件NetworkAnimator，注意！预制体一定是要打开状态，否则将会同步不成功！然后，将玩家预制体身上的Rigidbody组件的Is Kinematic属性先勾选上，等待玩家初始化完毕后再打开，否则就会有玩家出生位置错误的问题
因此，我们需要修改一下PlayInit.cs脚本内容：

    private void OnStartGame()
    {
        //这里是服务端控制的所有客户端同时开始进入游戏的函数,因此应该使用OwnerClientId来
        //确保每一个客户端通过各自自己的唯一ID标识获得的是自己的各自信息
        //如果使用LocalClientId则这里同时所有客户端获得的信息都是服务端的信息导致所有客户端都长的和服务端一样
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[OwnerClientId];//OwnerClientId本人自己的ID，而不是本地客户端ID
        Transform body = transform.GetChild(playerInfo.gender);
        body.GetComponent<Rigidbody>().isKinematic = false;
        Transform other = transform.GetChild(1 - playerInfo.gender);
        other.gameObject.SetActive(false);//选择一个性别预制体后就关掉另外一个性别的预制体
        PlayerSync playerSync = GetComponent<PlayerSync>();
        playerSync.SetTarget(playerInfo.gender);
        playerSync.enabled = true;
        //因为多人游戏中有多个客户端,添加IsLocalPlayer条件是为了每一个客户端只执行自己客户端的代码,而不去调用其他客户端的代码
        if (IsLocalPlayer)//IsLocalPlayer判断是否是本地客户端,只有本地客户端执行此代码
        {
            GameCtrl.Instance.SetFollowTarget(body);//将摄像机对准玩家
            body.GetComponent<PlayerMove>().enabled = true;//只有当客户端知道自己是自己的时候才启用控制移动函数脚本
        }
        transform.position = GameCtrl.Instance.GetSpawnPos();
    }

简而言之，同步玩家动画只需要使用NGO自带的NetworkAnimator组件将其添加至角色预制体，再将预制体的Animtor组件拖入即可(注意此处有BUG！有时会导致客户端的动画无法同步到服务器，因此弃用！更新后的方法查看23章节)
需要注意的是预制体一定要在编辑器中默认打开(激活)状态
===================================================================================================================================================================

===================================================================================================================================================================
22.添加敌人及敌人动画

配置敌人的动画控制器
===================================================================================================================================================================

===================================================================================================================================================================
23.解决动画同步问题

解决更新21章动画同步问题：
新建一个自定义脚本OwnerAnimator.cs并删除角色预制体上的NetworkAnimoter组件，OwnerAnimator.cs内容：

using Unity.Netcode.Components;

public class OwnerAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}

该自定义脚本继承于NetworkAnimator并且重写OnIsServerAuthoritative()函数并返回false即可实现和NetworkAnimator组件相同的动画同步方法且没有BUG！
===================================================================================================================================================================
未完待续...
