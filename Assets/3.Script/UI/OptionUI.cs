using UnityEngine;

public class OptionUI : MonoBehaviour
{
    public GameObject optionpannel;
    //옵션창 열기
    public void OpenOP()
    {
        optionpannel.SetActive(true);
    }
    //옵션창 닫기
    public void CloseOP()
    {
        optionpannel.SetActive(false);
    }
}
