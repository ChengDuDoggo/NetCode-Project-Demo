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
