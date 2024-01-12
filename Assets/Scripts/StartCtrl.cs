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
    /// 加入主机房间当客户端
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
