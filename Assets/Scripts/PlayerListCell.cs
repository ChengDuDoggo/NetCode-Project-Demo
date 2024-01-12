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
        _name.text = playerInfo.name;
        _ready.text = playerInfo.isready ? "准备" : "未准备";
        _gender.text = playerInfo.gender == 0 ? "男" : "女";
    }
    public void UpdateInfo(PlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
        _name.text = playerInfo.name;
        _ready.text = PlayerInfo.isready ? "准备" : "未准备";
        _gender.text = PlayerInfo.gender == 0 ? "男" : "女";
    }
    internal void SetReady(bool arg0)
    {
        _ready.text = arg0 ? "准备" : "未准备";
    }
}
