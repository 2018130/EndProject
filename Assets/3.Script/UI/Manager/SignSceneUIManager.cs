using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignSceneUIManager : MonoBehaviour
{
    public static SignSceneUIManager Instance { get; set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Sign Up"), Space]
    [SerializeField]
    private TMP_InputField  signUpId_InputField;
    [SerializeField]
    private TMP_InputField signUpPwd_InputField;
    [SerializeField]
    private Button checkId_Btn;
    [SerializeField]
    private Button signUp_Btn;

    [Space]

    [SerializeField]
    private TMP_Text error_Text;

    [Header("Nickname"), Space]
    [SerializeField]
    private GameObject nicknamePopup;
    [SerializeField]
    private TMP_InputField nickname_InputField;
    [SerializeField]
    private Button nicknameCheck_Btn;
    [SerializeField]
    private Button nicknameConfirm_Btn;
    [SerializeField]
    private TMP_Text nicknameError_Text;

    [Header("SignIn"), Space]
    [SerializeField]
    private TMP_InputField signInId_InputField;
    [SerializeField]
    private TMP_InputField signInPwd_InputField;
    [SerializeField]
    private Button signIn_Btn;

    private void Start()
    {
        checkId_Btn.onClick.AddListener(() => {
            ClientTCP.Instance.CheckId(signUpId_InputField.text);
        });

        signUp_Btn.onClick.AddListener(() =>
        {
            ClientTCP.Instance.SetNickname(signUpId_InputField.text, signUpPwd_InputField.text, nickname_InputField.text);
        });

        nicknameCheck_Btn.onClick.AddListener(() =>
        {
            ClientTCP.Instance.CheckNickname(signUpId_InputField.text, signUpPwd_InputField.text, nickname_InputField.text);
        });

        nicknameConfirm_Btn.onClick.AddListener(() =>
        {
            //ClientTCP.Instance.SetNickname(signUpId_InputField.text, signUpPwd_InputField.text, nickname_InputField.text);
            ToggleNicknamePopup();
        });

        signIn_Btn.onClick.AddListener(() =>
        {
            ClientTCP.Instance.SignIn(signInId_InputField.text, signInPwd_InputField.text);
        });
    }

    public void SetInteractIdInputField(bool active)
    {
        signUpId_InputField.interactable = active;
    }

    public void SetErrorText(string text)
    {
        error_Text.text = text;
    }


    public void ToggleNicknamePopup()
    {
        nicknamePopup.SetActive(!nicknamePopup.activeSelf);
    }

    public void SetInteractNicknameInputField(bool active)
    {
        nickname_InputField.interactable = active;
    }

    public void SetNicknameErrorText(string text)
    {
        nicknameError_Text.text = text;
    }
}
