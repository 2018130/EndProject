using UnityEngine;
using TMPro;

public class PlayerActionPromptUI : MonoBehaviour
{
    public static PlayerActionPromptUI Instance { get; private set; }

    [SerializeField] private GameObject promptRoot; // 이미지 1개짜리 오브젝트
    [SerializeField] private TMP_Text promptText;   // 안에 들어갈 텍스트

    [SerializeField] private Vector3 headOffset = new Vector3(0, 2.5f, 0);

    private Camera mainCam;
    private Transform target;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        promptRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (mainCam == null) mainCam = Camera.main;

        Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + headOffset);

        if (screenPos.z < 0)
        {
            HideAllPrompts();
            return;
        }

        promptRoot.GetComponent<RectTransform>().position = screenPos;
    }

    public void ShowPrompts(Transform t, bool showRevive, bool showExecute)
    {
        target = t;
        promptRoot.SetActive(true);

        string text = "";
        if (showRevive) text += "R\n";
        if (showExecute) text += "E\n";

        promptText.text = text.TrimEnd('\n');
    }

    public void HideAllPrompts()
    {
        target = null;
        promptRoot.SetActive(false);
    }
}