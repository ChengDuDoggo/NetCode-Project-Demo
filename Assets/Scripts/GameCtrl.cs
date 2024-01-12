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
    public Vector3 GetSpawnPos()
    {
        Vector3 pos = new Vector3();
        Vector3 offset = transform.forward * Random.Range(-10.0f, 10.0f) + transform.right * Random.Range(-10.0f, 10.0f);//获得偏移值
        pos = transform.position + offset;
        return pos;
    }
}
