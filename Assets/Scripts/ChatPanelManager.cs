using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ChatInfo
{
    public string chatKey { set; get; }
    public string chatSendTime { set; get; }
    public string chatUser { set; get; }
    public string chatText { set; get; }
    public string chatEdited { set; get; }
    public string chatEditing { set; get; }
    public string chatEditTime { set; get; }
    public bool isSystemMessage { set; get; }
}

public class ChatPanelManager : MonoBehaviour {

    public GameObject chatItemObj;
    public GameObject chatListObj;

    public UIInput mInput;

    GameObject loginPanel;
    GameObject roomPanel;
    GameObject chatPanel;
    
    List<ChatInfo> m_ChatList = new List<ChatInfo>();

    string editChatKey = null;
    string editChatUser = null;
    string editChatTextBefore = null;

	// Use this for initialization
	void Start ()
    {
        mInput.label.maxLineCount = 1;

        loginPanel = transform.parent.Find("LoginPanel").gameObject;
        roomPanel = transform.parent.Find("RoomPanel").gameObject;
        chatPanel = transform.parent.Find("ChatPanel").gameObject;
    }
	
	// Update is called once per frame
	void Update () {
        if(m_ChatList.Count > 0)
        {
            foreach (ChatInfo eachInfo in m_ChatList)
                AddChatToChatView(eachInfo);
            chatListObj.GetComponent<UIGrid>().Reposition();
            chatListObj.transform.parent.GetComponent<UIScrollView>().ResetPosition();
            chatListObj.SetActive(false);
            chatListObj.SetActive(true);
            ClearChatList();
        }
    }

    private void OnDisable()
    {
        int childCnt = chatListObj.transform.childCount;
        for (int i = childCnt - 1; i >= 0; i--)
            Destroy(chatListObj.transform.GetChild(i).gameObject);

        string userName = AppManager.Instance.user.DisplayName;
        string currentRoomNo = AppManager.Instance.currentRoomNo;

        DatabaseReference newSystemChatRef = FirebaseDatabase.DefaultInstance.RootReference.Child("Messages").Child(currentRoomNo).Push();
        newSystemChatRef.Child(userName).SetValueAsync("out");
        newSystemChatRef.Child("SYSTEM").SetValueAsync("TRUE");
        newSystemChatRef.Child("SendTime").SetValueAsync(ServerValue.Timestamp);
    }

    private void OnEnable()
    {
        editChatTextBefore = null;
        editChatKey = null;
        editChatUser = null;

        ClearChatList();
        string userName = AppManager.Instance.user.DisplayName;
        string currentRoomNo = AppManager.Instance.currentRoomNo;

        mInput.value = "";

        FirebaseDatabase.DefaultInstance.GetReference("Messages").Child(currentRoomNo).ChildAdded += HandleMessageAdded;
        FirebaseDatabase.DefaultInstance.GetReference("Messages").Child(currentRoomNo).ChildChanged += HandleMessageChanged;

        DatabaseReference newSystemChatRef = FirebaseDatabase.DefaultInstance.RootReference.Child("Messages").Child(currentRoomNo).Push();
        newSystemChatRef.Child(userName).SetValueAsync("in");
        newSystemChatRef.Child("SYSTEM").SetValueAsync("TRUE");
        newSystemChatRef.Child("SendTime").SetValueAsync(ServerValue.Timestamp);

        RefreshBG_Info();
    }

    void HandleMessageAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        int nDbIterator = 0;
        //Debug.Log("HandleMessageAdded " + args.Snapshot);
        ChatInfo newChatInfo = new ChatInfo();
        newChatInfo.chatKey = args.Snapshot.Key;
        newChatInfo.chatEdited = null;
        newChatInfo.chatEditing = null;
        newChatInfo.isSystemMessage = false;
        foreach (DataSnapshot aaaChat in args.Snapshot.Children)
        {
            //Debug.Log(aaaChat);
            if (aaaChat.Key.Equals("SendTime"))
                newChatInfo.chatSendTime = aaaChat.Value.ToString();
            else if(aaaChat.Key.Equals("Edited"))
                newChatInfo.chatEdited = (string)aaaChat.Value;
            else if (aaaChat.Key.Equals("Editing"))
                newChatInfo.chatEditing = (string)aaaChat.Value;
            else if (aaaChat.Key.Equals("EditTime"))
                newChatInfo.chatEditTime = aaaChat.Value.ToString();
            else if (aaaChat.Key.Equals("SYSTEM"))
                newChatInfo.isSystemMessage = true;
            else if (!aaaChat.Key.Equals("EditHistory"))
            {
                newChatInfo.chatUser = aaaChat.Key;
                newChatInfo.chatText = (string)aaaChat.Value;
            }

            nDbIterator++;
        }
        m_ChatList.Add(newChatInfo);
    }

    void HandleMessageChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        int nDbIterator = 0;
        //Debug.Log("HandleMessageChanged " + args.Snapshot);
        ChatInfo newChatInfo = new ChatInfo();
        newChatInfo.chatKey = args.Snapshot.Key;
        newChatInfo.chatEdited = null;
        newChatInfo.chatEditing = null;
        newChatInfo.isSystemMessage = false;
        foreach (DataSnapshot bbbChat in args.Snapshot.Children)
        {
            //Debug.Log(bbbChat);
            if (bbbChat.Key.Equals("SendTime"))
                newChatInfo.chatSendTime = bbbChat.Value.ToString();
            else if (bbbChat.Key.Equals("Edited"))
                newChatInfo.chatEdited = (string)bbbChat.Value;
            else if (bbbChat.Key.Equals("Editing"))
                newChatInfo.chatEditing = (string)bbbChat.Value;
            else if (bbbChat.Key.Equals("EditTime"))
                newChatInfo.chatEditTime = bbbChat.Value.ToString();
            else if (bbbChat.Key.Equals("SYSTEM"))
                newChatInfo.isSystemMessage = true;
            else if (!bbbChat.Key.Equals("EditHistory"))
            {
                newChatInfo.chatUser = bbbChat.Key;
                newChatInfo.chatText = (string)bbbChat.Value;
            }

            nDbIterator++;
        }
        m_ChatList.Add(newChatInfo);
    }

    void ClearChatList()
    {
        m_ChatList.Clear();
    }

    void AddChatToChatView(ChatInfo _chatInfo)
    {
        string chatkey = _chatInfo.chatKey;
        string username = _chatInfo.chatUser;
        string text = _chatInfo.chatText;
        string editedStr = _chatInfo.chatEdited;
        string editingStr = _chatInfo.chatEditing;
        string sendTime = _chatInfo.chatSendTime;
        string editTime = _chatInfo.chatEditTime;
        bool isSystem = _chatInfo.isSystemMessage;

        bool bOldItem = false;
        for(int i=0; i<chatListObj.transform.childCount; i++)
        {
            if(chatListObj.transform.GetChild(i).gameObject.name.Equals(chatkey))
            {
                bOldItem = true;
                break;
            }
        }
        
        if (bOldItem)
        {
            GameObject tempItemObj = chatListObj.transform.Find(chatkey).gameObject;
            InitValueWithChatList(tempItemObj,chatkey,username,text,editedStr,editingStr,sendTime,editTime,isSystem);
        }
        else
        {
            if (chatListObj.transform.childCount >= 10)
                DestroyImmediate(chatListObj.transform.GetChild(0).gameObject);

            GameObject tempItemObj = NGUITools.AddChild(chatListObj, chatItemObj);
            tempItemObj.transform.localScale = Vector3.one;
            tempItemObj.SetActive(true);
            InitValueWithChatList(tempItemObj,chatkey,username,text,editedStr,editingStr,sendTime,editTime, isSystem);
        }
    }

    void InitValueWithChatList(GameObject tempItemObj,string chatkey,string username,string text,string editedStr,string editingStr,string sendTime,string editTime,bool bSystem)
    {
        if (AppManager.Instance.user.DisplayName.Equals(username))
            username = "YOU";

        if (bSystem)
        {
            tempItemObj.transform.Find("Name").gameObject.SetActive(false);
            tempItemObj.transform.Find("Edit").gameObject.SetActive(false);
            tempItemObj.transform.Find("Pencil").gameObject.SetActive(false);
            tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().color = new Color(0.5f, 0.5f, 0.5f);

            sendTime = AppManager.Instance.GetDateTimeFromTimeStamp(sendTime);
            tempItemObj.transform.Find("Sendtime").gameObject.GetComponent<UILabel>().text = sendTime;
            if (text.Equals("in"))
                tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().text = username + " joined this room";
            else if (text.Equals("out"))
                tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().text = username + " left this room";
            else if (text.Equals("collaborator"))
                tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().text = username + " set as a collaborator";
            else if (text.Equals("viewer"))
                tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().text = username + " set as a viewer";
            tempItemObj.name = chatkey;
        }
        else
        {
            tempItemObj.transform.Find("Name").gameObject.SetActive(true);
            tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().color = new Color(0.12f, 0.7f, 0.5f);

            tempItemObj.transform.Find("Name").gameObject.GetComponent<UILabel>().text = username + ":";

            sendTime = AppManager.Instance.GetDateTimeFromTimeStamp(sendTime);
            tempItemObj.transform.Find("Sendtime").gameObject.GetComponent<UILabel>().text = sendTime;

            tempItemObj.transform.Find("Text").gameObject.GetComponent<UILabel>().text = text;
            if (editingStr != null)
            {
                tempItemObj.transform.Find("Edit").gameObject.SetActive(true);
                tempItemObj.transform.Find("Edit").gameObject.GetComponent<UILabel>().text = editingStr + " is editing now...";
            }
            else if (editedStr != null)
            {
                tempItemObj.transform.Find("Edit").gameObject.SetActive(true);
                if (editedStr.Equals(AppManager.Instance.user.DisplayName))
                    editedStr = "YOU";
                editTime = AppManager.Instance.GetDateTimeFromTimeStamp(editTime);
                tempItemObj.transform.Find("Edit").gameObject.GetComponent<UILabel>().text = editedStr + " edited (" + editTime + ")";
            }
            else
                tempItemObj.transform.Find("Edit").gameObject.SetActive(false);
            tempItemObj.name = chatkey;
        }
    }

    public void OnBackBtnClicked()
    {
        if (editChatKey != null)
            return;
        loginPanel.SetActive(false);
        roomPanel.SetActive(true);
        chatPanel.SetActive(false);
    }

    public void OnSubmit()
    {
        string currentRoomNo = AppManager.Instance.currentRoomNo;
        string userId = AppManager.Instance.user.UserId;
        string userName = AppManager.Instance.user.DisplayName;
        // It's a good idea to strip out all symbols as we don't want user input to alter colors, add new lines, etc
        string text = NGUIText.StripSymbols(mInput.value);

        if (!AppManager.Instance.GetUserType(currentRoomNo))
            return;

        if (!string.IsNullOrEmpty(text))
        {
            mInput.value = "";
            // mInput.isSelected = false;

            DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

            if (editChatKey != null)
            {
                reference.Child("Messages").Child(currentRoomNo).Child(editChatKey).Child("Editing").RemoveValueAsync();
                if(!editChatTextBefore.Equals(text))
                {
                    DatabaseReference existChatRef = reference.Child("Messages").Child(currentRoomNo).Child(editChatKey);
                    existChatRef.Child(editChatUser).SetValueAsync(text);
                    existChatRef.Child("Edited").SetValueAsync(AppManager.Instance.user.DisplayName);
                    existChatRef.Child("EditTime").SetValueAsync(ServerValue.Timestamp);

                    DatabaseReference chatHistoryRef = existChatRef.Child("EditHistory").Push();
                    chatHistoryRef.Child(userName).SetValueAsync(text);
                    chatHistoryRef.Child("EditTime").SetValueAsync(ServerValue.Timestamp);
                }

                editChatTextBefore = null;
                editChatKey = null;
                editChatUser = null;
            }
            else
            {
                DatabaseReference newChatRef = reference.Child("Messages").Child(currentRoomNo).Push();
                newChatRef.Child(userName).SetValueAsync(text);
                newChatRef.Child("SendTime").SetValueAsync(ServerValue.Timestamp);
            }

        }
    }

    public void OnPencilClicked(GameObject obj)
    {
        if (editChatKey != null)
        {
            Debug.Log("You are editing other one now...");
            return;
        }
        string editStr = obj.transform.Find("Edit").gameObject.GetComponent<UILabel>().text;
        if(editStr.Contains("is editing now..."))
        {
            Debug.Log("Someone is editing this now...");
            return;
        }

        editChatKey = obj.name;
        Debug.Log("OnPencilClicked " + editChatKey);
        editChatUser = obj.transform.Find("Name").gameObject.GetComponent<UILabel>().text;
        editChatUser = editChatUser.Substring(0, editChatUser.Length - 1);
        if (editChatUser.Equals("YOU"))
            editChatUser = AppManager.Instance.user.DisplayName;
        string currentRoomNo = AppManager.Instance.currentRoomNo;
        string userName = AppManager.Instance.user.DisplayName;

        mInput.value = obj.transform.Find("Text").gameObject.GetComponent<UILabel>().text;
        editChatTextBefore = obj.transform.Find("Text").gameObject.GetComponent<UILabel>().text;

        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        reference.Child("Messages").Child(currentRoomNo).Child(editChatKey).Child("Editing").SetValueAsync(userName);
    }

    private void OnApplicationQuit()
    {
        string currentRoomNo = AppManager.Instance.currentRoomNo;
        if (editChatKey != null)
        {
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
            reference.Child("Messages").Child(currentRoomNo).Child(editChatKey).Child("Editing").RemoveValueAsync();
        }
    }
    
    public void OnUserTypeBtnClicked()
    {
        string userId = AppManager.Instance.user.UserId;
        string userName = AppManager.Instance.user.DisplayName;
        string currentRoomNo = AppManager.Instance.currentRoomNo;

        bool bUserType = AppManager.Instance.GetUserType(currentRoomNo);

        DatabaseReference newSystemChatRef = FirebaseDatabase.DefaultInstance.RootReference.Child("Messages").Child(currentRoomNo).Push();
        string val = bUserType ? "collaborator" : "viewer";
        newSystemChatRef.Child(userName).SetValueAsync(val);
        newSystemChatRef.Child("SYSTEM").SetValueAsync("TRUE");
        newSystemChatRef.Child("SendTime").SetValueAsync(ServerValue.Timestamp);

        if (bUserType)
            FirebaseDatabase.DefaultInstance.RootReference.Child("Users").Child(userId).Child(currentRoomNo).SetValueAsync("0");
        else
            FirebaseDatabase.DefaultInstance.RootReference.Child("Users").Child(userId).Child(currentRoomNo).SetValueAsync("1");
        AppManager.Instance.bUserType[currentRoomNo] = !bUserType;
        RefreshBG_Info();
    }

    private void RefreshBG_Info()
    {
        string currentRoomNo = AppManager.Instance.currentRoomNo;
        transform.Find("BG_Info/RoomNum").gameObject.GetComponent<UILabel>().text = currentRoomNo;
        bool bUserType = AppManager.Instance.GetUserType(currentRoomNo);
        if(bUserType)
        {
            transform.Find("UserTypeBtn").gameObject.transform.GetChild(0).gameObject.GetComponent<UILabel>().text = "To Viewer";
            transform.Find("BG_Info/UserType").gameObject.GetComponent<UILabel>().text = "You are a Collaborator now";
        }
        else
        {
            transform.Find("UserTypeBtn").gameObject.transform.GetChild(0).gameObject.GetComponent<UILabel>().text = "To Collaborator";
            transform.Find("BG_Info/UserType").gameObject.GetComponent<UILabel>().text = "You are a Viewer now";
        }
        mInput.gameObject.GetComponent<BoxCollider>().enabled = bUserType;
    }
}
