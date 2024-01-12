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
