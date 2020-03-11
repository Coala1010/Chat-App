using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPanelManager : MonoBehaviour {

    public GameObject roomItemObj;
    public GameObject roomListObj;
    List<string> roomList = new List<string>();

    GameObject loginPanel;
    GameObject roomPanel;
    GameObject chatPanel;
    GameObject usernameObj;
    public GameObject createroomBtn;
    public GameObject signoutBtn;

    bool bUserDataRefreshed = false;

    // Use this for initialization
    void Start () {

        loginPanel = transform.parent.Find("LoginPanel").gameObject;
        roomPanel = transform.parent.Find("RoomPanel").gameObject;
        chatPanel = transform.parent.Find("ChatPanel").gameObject;
        usernameObj = transform.Find("UserNameLabel").gameObject;
    }
	
	// Update is called once per frame
	void Update () {
        if(roomList.Count > 0)
        {
            for (int i = 0; i < roomList.Count; i++)
            {
                AddRoomToScrollView(roomList[i]);
            }
            roomListObj.GetComponent<UIGrid>().Reposition();
            roomListObj.SetActive(false);
            roomListObj.SetActive(true);
            roomList.Clear();
        }
        if(bUserDataRefreshed)
        {
            bUserDataRefreshed = false;
            createroomBtn.GetComponent<BoxCollider>().enabled = true;
            signoutBtn.GetComponent<BoxCollider>().enabled = true;
            usernameObj.GetComponent<UILabel>().text = "User Name : " + AppManager.Instance.user.DisplayName;
        }
    }

    private void OnEnable()
    {
        roomList.Clear();
        createroomBtn.GetComponent<BoxCollider>().enabled = false;
        signoutBtn.GetComponent<BoxCollider>().enabled = false;
        FirebaseDatabase.DefaultInstance.RootReference.Child("Messages").ChildAdded += HandleChildAdded;

        FirebaseDatabase.DefaultInstance.GetReference("Users").GetValueAsync().ContinueWith(task =>
        {
            if(task.IsFaulted)
            {
                Debug.LogError("Get Users Failed" + task.Exception);
            }
            else if(task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (DataSnapshot aaaChat in snapshot.Children)
                {
                    if (AppManager.Instance.user.UserId.Equals(aaaChat.Key))
                    {
                        bUserDataRefreshed = true;
                        foreach (DataSnapshot bbbChat in aaaChat.Children)
                            AppManager.Instance.bUserType[bbbChat.Key] = ((string)bbbChat.Value).Equals("1");
                        break;
                    }
                }
            }
        });
    }

    private void OnDisable()
    {
        int childCnt = roomListObj.transform.childCount;
        for(int i=childCnt-1; i>=0; i--)
            Destroy(roomListObj.transform.GetChild(i).gameObject);
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if(args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        roomList.Add(args.Snapshot.Key);
    }

    void AddRoomToScrollView(string roomNo)
    {
        GameObject tempItemObj = NGUITools.AddChild(roomListObj, roomItemObj);
        tempItemObj.transform.GetChild(0).gameObject.GetComponent<UILabel>().text = roomNo;
        tempItemObj.name = roomNo;
        tempItemObj.SetActive(true);
    }

    public void OnCreateRoom()
    {
        if(roomListObj.transform.childCount >= 20)
        {
            Debug.Log("Room List Limited");
            return;
        }
        int newRoomIndex = roomListObj.transform.childCount + 1;
        
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("Messages");
        reference.Child("Room" + newRoomIndex).SetValueAsync(AppManager.Instance.user.UserId);
    }

    public void OnRoomClicked(GameObject itemObj)
    {
        AppManager.Instance.currentRoomNo = itemObj.name;
        GotoChatPanel();
    }

    void GotoChatPanel()
    {
        loginPanel.SetActive(false);
        roomPanel.SetActive(false);
        chatPanel.SetActive(true);
    }
}
