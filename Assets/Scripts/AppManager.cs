using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Unity.Editor;
using Firebase.Database;
using System;

public class AppManager : MonoBehaviour {

    public static AppManager Instance;

    public GameObject usernameInput;
    public GameObject emailInput;
    public GameObject passwordInput;
    GameObject loginPanel;
    GameObject roomPanel;
    GameObject chatPanel;

    public Firebase.Auth.FirebaseAuth auth;
    public Firebase.Auth.FirebaseUser user;

    public Dictionary<string,bool> bUserType; // 0: Viewer, 1: Collaborator

    public string currentRoomNo;
    int sceneFlag = 0;

    GameObject signinBtn;
    GameObject signupBtn;

    bool bSignActionFinished = false;

    // Use this for initialization
    void Start () {
        Instance = this;
        bUserType = new Dictionary<string, bool>();

        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://vitaliy-chat.firebaseio.com/");
        
        loginPanel = transform.Find("LoginPanel").gameObject;
        roomPanel = transform.Find("RoomPanel").gameObject;
        chatPanel = transform.Find("ChatPanel").gameObject;
        signinBtn = loginPanel.transform.Find("SignIn").gameObject;
        signupBtn = loginPanel.transform.Find("SignUp").gameObject;
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                Debug.Log("Firebase is Ready");

                // Set a flag here to indicate whether Firebase is ready to use by your app.
                auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

                sceneFlag = 1;
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    public bool GetUserType(string roomId)
    {
        if(bUserType.ContainsKey(roomId))
            return bUserType[roomId];
        return true;
    }

    public string GetDateTimeFromTimeStamp(string timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return string.Empty;
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        dateTime = dateTime.AddSeconds((double)(long.Parse(timestamp) / 1000));
        return dateTime.ToString("MM-dd HH:mm");
    }

    public void OnSignOutBtnClicked()
    {
        auth.SignOut();
        sceneFlag = 1;
    }

    void SetColliderActive(bool bEnable)
    {
        signinBtn.GetComponent<BoxCollider>().enabled = bEnable;
        signupBtn.GetComponent<BoxCollider>().enabled = bEnable;
    }

    void UserSignIn(string mailStr, string pwd)
    {
        SetColliderActive(false);
        auth.SignInWithEmailAndPasswordAsync(mailStr, pwd).ContinueWith(task =>
        {
            bSignActionFinished = true;
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                return;
            }

            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            sceneFlag = 2;
            user = auth.CurrentUser;
        });
    }

    void UserSignUp(string usernameStr, string mailStr, string pwdStr)
    {
        SetColliderActive(false);
        auth.CreateUserWithEmailAndPasswordAsync(mailStr, pwdStr).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                return;
            }

            // Firebase user has been created.
            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);

            FirebaseDatabase.DefaultInstance.RootReference.Child("Users").Child(newUser.UserId).SetValueAsync("1");
            SetProfileAsCollaborate(newUser, usernameStr, mailStr, pwdStr);
        });
    }

    void SetProfileAsCollaborate(Firebase.Auth.FirebaseUser _user, string usernameStr, string mailStr, string pwdStr)
    {
        if (_user != null)
        {
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
            {
                DisplayName = usernameStr,
            };
            _user.UpdateUserProfileAsync(profile).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("UpdateUserProfileAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    return;
                }

                Debug.Log("User Profile update successfully.");
                UserSignIn(mailStr, pwdStr);
            });
        }
    }

    public void OnSignInBtnClicked()
    {
        string emailStr = emailInput.GetComponent<UIInput>().value;
        string pwdStr = passwordInput.GetComponent<UIInput>().value;
        if(string.IsNullOrEmpty(emailStr) || string.IsNullOrEmpty(pwdStr))
        {
            Debug.LogError("Email or Password is Empty");
            return;
        }

        UserSignIn(emailStr, pwdStr);
    }

    public void OnSignUpBtnClicked()
    {
        string usernameStr = usernameInput.GetComponent<UIInput>().value;
        string emailStr = emailInput.GetComponent<UIInput>().value;
        string pwdStr = passwordInput.GetComponent<UIInput>().value;
        if (string.IsNullOrEmpty(emailStr) || string.IsNullOrEmpty(pwdStr) || string.IsNullOrEmpty(usernameStr))
        {
            Debug.LogError("Username or Email or Password is Empty");
            return;
        }

        UserSignUp(usernameStr, emailStr, pwdStr);
    }

    public void GotoLoginPanel()
    {
        loginPanel.SetActive(true);
        roomPanel.SetActive(false);
        chatPanel.SetActive(false);

        InitValue();
    }

    public void GotoRoomPanel()
    {
        loginPanel.SetActive(false);
        roomPanel.SetActive(true);
        chatPanel.SetActive(false);

        string nameStr = user.DisplayName;
        string mailStr = emailInput.GetComponent<UIInput>().value;
        string pwdStr = passwordInput.GetComponent<UIInput>().value;
        PlayerPrefs.SetString("prefs_name", nameStr);
        PlayerPrefs.SetString("prefs_email", mailStr);
        PlayerPrefs.SetString("prefs_pwd", pwdStr);
    }

    // Update is called once per frame
    void Update () {

        if(sceneFlag == 1)
        {
            GotoLoginPanel();
            sceneFlag = 0;
        }
        else if (sceneFlag == 2)
        {
            GotoRoomPanel();
            sceneFlag = 0;
        }
        if(bSignActionFinished)
        {
            bSignActionFinished = false;
            SetColliderActive(true);
        }
    }

    void InitValue()
    {
        string name_str = PlayerPrefs.GetString("prefs_name");
        string email_str = PlayerPrefs.GetString("prefs_email");
        string pwd_str = PlayerPrefs.GetString("prefs_pwd");

        usernameInput.GetComponent<UIInput>().value = name_str;
        emailInput.GetComponent<UIInput>().value = email_str;
        passwordInput.GetComponent<UIInput>().value = pwd_str;
    }
}
