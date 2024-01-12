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
