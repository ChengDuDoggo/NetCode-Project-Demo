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
}
