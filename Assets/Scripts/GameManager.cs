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
