﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class InfoManager : MonoBehaviour
{
    #region public fields

    public delegate void VoidDelegate(bool value);
    public event VoidDelegate OnJournalOpen; //event on journal open

    public Dictionary<string, Task> CurrentTasks; //current tasks list
    public Dictionary<string, Task> CompletedTasks; //complete task list

    #endregion

    #region private fields

    #region serialize fields

    [SerializeField] public GameObject m_JournalUI; //journal ui
    [SerializeField] private GameObject m_Bookmarks; //bookmark list
    [SerializeField] private GameObject m_Map;
    [SerializeField] private Transform m_Content; //buttons grid
    [SerializeField] private Image m_BatteryImage;
    [SerializeField] private GameObject m_Seperator;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI m_HeaderText;
    [SerializeField] private TextMeshProUGUI m_ScrapText;
    [SerializeField] private TextMeshProUGUI m_DateText;
    [SerializeField] private TextMeshProUGUI m_BatteryText;
    [SerializeField] private GameObject m_NoItemsText;
    [SerializeField] private TextMeshProUGUI m_MainText;

    [Header("Effects")]
    [SerializeField] private Audio m_OpenAudio;
    [SerializeField] private Audio m_CloseAudio;

    #endregion

    public static bool IsJournalOpen
    {
        get
        {
            if (Instance != null)
            {
                return Instance.m_JournalUI.activeSelf;
            }

            return false;
        }
    }

    private int m_CurrentOpenBookmark = 0; //current open tab
    private Dictionary<int, List<Button>> m_ButtonsList; //buttons list
    private static Sprite[] itemsSpriteAtlas; //atals for items
    private bool m_IsCantOpenJournal;

    #endregion

    #region private methods

    #region Singleton

    public static InfoManager Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this);

            m_ButtonsList = new Dictionary<int, List<Button>>();
            InitializeButtonsDictionary();
        }
    }

    #endregion

    #region Initialize

    // Use this for initialization
    private void Start()
    {

        PauseMenuManager.Instance.OnGamePause += SetIsCantOpenJournal;

        m_JournalUI.SetActive(false); //hide journal ui

        if (CurrentTasks == null)
            CurrentTasks = new Dictionary<string, Task>(); //initialize current tasks

        if (CompletedTasks == null)
            CompletedTasks = new Dictionary<string, Task>(); //initialize completed tasks

        itemsSpriteAtlas = Resources.LoadAll<Sprite>("Items/items1"); //initialize items atlas

        m_DateText.text = GetDate();

        SetBatteryStatus();
    }

    private void InitializeButtonsDictionary()
    {
        for (int index = 0; index < m_Bookmarks.transform.childCount; index++)  //initialize buttons list for each bookmark
        {
            m_ButtonsList.Add(index, new List<Button>());
        }
    }

    #endregion

    // Update is called once per frame
    private void Update()
    {
        if (!m_IsCantOpenJournal)
        {
            //if journal is opened
            if (m_JournalUI.activeSelf)
            {
                if (InputControlManager.Instance.IsBackMenuPressed() || InputControlManager.Instance.IsBackPressed())
                {
                    StartCoroutine ( CloseJournalWithDelay() );
                }

                if (InputControlManager.Instance.IsLeftBumperPressed()) //move to top
                {
                    if (m_CurrentOpenBookmark > 0)
                    {
                        InfoManagement(m_CurrentOpenBookmark - 1);
                    }
                }
                else if (InputControlManager.Instance.IsRightBumperPressed()) //move to bottom
                {
                    if (m_CurrentOpenBookmark < 3)
                    {
                        InfoManagement(m_CurrentOpenBookmark + 1);
                    }
                }
            }
            else if (InputControlManager.Instance.IsBackMenuPressed()) //open journal
            {
                InfoManagement(m_CurrentOpenBookmark);
            }
        }
    }

    private void InfoManagement(int id)
    {
        OpenBookmark(id);

        OpenJournal();

        if (m_ButtonsList[id].Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(m_ButtonsList[id][0].gameObject);
        }
    }

    private void OpenJournal()
    {
        Time.timeScale = 0f; //stop time

        AudioManager.Instance.Play(m_OpenAudio);
        m_JournalUI.SetActive(true);

        m_MainText.text = string.Empty;
        m_ScrapText.text = "Scrap amount - <color=yellow>" + PlayerStats.Scrap.ToString() + "</color>";

        OnJournalOpen(m_JournalUI.activeSelf); //notify that journal open/close
    }

    private void ChangeButtonsVisibility(bool value, IEnumerable<Button> buttons)
    {
        foreach (var item in buttons)
        {
            item.gameObject.SetActive(value);
        }
    }

    private Button CreateTaskButton(string name)
    {
        var buttonFromResources = Resources.Load("Managers/InfoManager/TaskButton") as GameObject;
        var instantiateTaskButton = Instantiate(buttonFromResources, m_Content);

        instantiateTaskButton.name = name; //set button name to task
        instantiateTaskButton.GetComponentInChildren<TextMeshProUGUI>().text =
            LocalizationManager.Instance.GetJournalLocalizedValue(name); //change button caption to the task name

        return instantiateTaskButton.GetComponent<Button>();
    }

    private Button CreateItemButton(ItemDescription item)
    {
        var resourceItemButton = Resources.Load("Managers/InfoManager/InventoryItem") as GameObject;
        var instantiateItemButton = Instantiate(resourceItemButton, m_Content);

        instantiateItemButton.name = LocalizationManager.Instance.GetItemsLocalizedValue(item.Name); //change button name to the item name

        instantiateItemButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text =
            LocalizationManager.Instance.GetItemsLocalizedValue(item.Name); //change button caption to the item name

        instantiateItemButton.transform.GetChild(1).GetComponent<Image>().sprite =
            itemsSpriteAtlas.SingleOrDefault(x => x.name == item.ImageInAtlas); //add item image from atlas

        instantiateItemButton.GetComponent<InventoryItem>().Initialize(item, m_MainText); //initialize item button info

        instantiateItemButton.SetActive(false); //hide button

        return instantiateItemButton.GetComponent<Button>();
    }

    //TODO: Localize text
    private string GetBookmarkname(int id)
    {
        var name = "NO NAME";

        switch (id)
        {
            case 0:
                name = "CURRENT TASKS";
                break;
            case 1:
                name = "COMPLETED TASKS";
                break;
            case 2:
                name = "INVENTORY";
                break;
            case 3:
                name = "LOCATION - <color=#45f442>" + GameMaster.Instance.SceneName + "</color>";
                break;
        }

        return name;
    }

    #endregion

    #region public methods

    public void OpenBookmark(int id)
    {
        //change bookmarks positions too show player what bookmark is currently open
        m_Bookmarks.transform.GetChild(m_CurrentOpenBookmark).GetComponent<Image>().color = new Color32(92, 92, 92, 255);
        m_Bookmarks.transform.GetChild(id).GetComponent<Image>().color = new Color32(255, 255, 225, 255);

        ChangeButtonsVisibility(false, m_ButtonsList[m_CurrentOpenBookmark]); //hide current bookmark buttons

        //disable decoration 
        m_NoItemsText.SetActive(false);
        m_Seperator.SetActive(false);

        if (id != 3)
        {
            m_Map.SetActive(false);

            if (m_ButtonsList[id].Count > 0)
            {
                m_Seperator.SetActive(true);
                ChangeButtonsVisibility(true, m_ButtonsList[id]); //show new bookmark buttons
            }
            else
                m_NoItemsText.SetActive(true);
        }
        else
        {
            m_Map.SetActive(true);
        }

        m_CurrentOpenBookmark = id; //change current book mark id
        m_MainText.text = string.Empty; //clear main text 
        m_HeaderText.text = GetBookmarkname(id);

        AudioManager.Instance.Play(m_OpenAudio); //play open journal sound

        InfoManagerLight.Instance?.ChangeLight(id);

        InputControlManager.Instance.StartGamepadVibration(1, .05f);
    }

    public IEnumerator CloseJournalWithDelay()
    {
        yield return null;

        CloseJournal();
    }

    public void CloseJournal()
    {
        Time.timeScale = 1f; //return time back to normal

        m_JournalUI.SetActive(false); //hide journal ui
        OnJournalOpen?.Invoke(false); //notify that journal is close

        AudioManager.Instance.Play(m_CloseAudio); //play open journal sound 

        if (InfoManagerLight.Instance != null)
            InfoManagerLight.Instance.ChangeLight(-1);
    }

    public void SetIsCantOpenJournal(bool value)
    {
        m_IsCantOpenJournal = value;
    }

    #endregion

    #region task methods

    public void DisplayTaskText(string taskName)
    {
        var value = string.Empty;

        if (CurrentTasks.ContainsKey(taskName)) //search task in current tasks
        {
            value = CurrentTasks[taskName].GetText(); //show task description
        }
        else if (CompletedTasks.ContainsKey(taskName)) //search task in completed tasks
        {
            value = CompletedTasks[taskName].GetText(); //show task description
        }

        m_MainText.text = value;
    }

    public bool AddTask(string taskName, string taskNameKey, string key)
    {
        if (!CurrentTasks.ContainsKey(taskName)) //add new task if it is not already added
        {
            m_ButtonsList[0].Add(CreateTaskButton(taskName)); //create new task button
            var newTask = new Task(taskName, taskNameKey, key); //create new task
            CurrentTasks.Add(taskName, newTask); //add task to the list

            return true; //task was successfuly added
        }

        return false; //can't add task
    }

    public void RecreateState()
    {
        if (CurrentTasks != null)
        {
            if (m_ButtonsList[0] != null) //if current task buttons is initialized
            {
                foreach (var item in CurrentTasks) //add all current tasks from the saved state
                {
                    m_ButtonsList[0].Add(CreateTaskButton(item.Value.NameKey));
                }
            }
        }

        if (CompletedTasks != null)
        {
            if (m_ButtonsList[1] != null) //if completed task buttons is initialized
            {
                foreach (var item in CompletedTasks) //add all completed tasks from the saved state
                {
                    m_ButtonsList[1].Add(CreateTaskButton(item.Key));
                }
            }
        }

        if (PlayerStats.PlayerInventory != null)
        {
            if (m_ButtonsList[2] != null) //if inventory buttons is initialized
            {
                foreach (var item in PlayerStats.PlayerInventory.m_Bag) //add all items from the saved state
                {
                    AddItem(item);
                }
            }
        }
    }

    public bool UpdateTask(string taskName, string key)
    {
        if (CurrentTasks.ContainsKey(taskName)) //if task in the journal
        {
            CurrentTasks[taskName].TaskUpdate(key); //updated task

            return true; //task was updated
        }

        return false; //can't updated task
    }

    public bool CompleteTask(string taskname, string key)
    {
        if (CurrentTasks.ContainsKey(taskname)) //if task is in the journal
        {
            CurrentTasks[taskname].TaskComplete(key); //complete task

            //move task from the current task to the completed
            CompletedTasks.Add(taskname, CurrentTasks[taskname]);
            CurrentTasks.Remove(taskname);

            //move task button from current list to the completed
            var currentTaskButton = m_ButtonsList[0].FirstOrDefault(x => x.name == taskname);
            m_ButtonsList[1].Add(currentTaskButton);
            m_ButtonsList[0].Remove(currentTaskButton);

            OpenBookmark(1);

            return true; //task was successfuly updated
        }

        return false; //can't updated task
    }

    #endregion

    #region inventory methods

    public void AddItem(ItemDescription item)
    {
        var button = CreateItemButton(item); //create new item button

        m_ButtonsList[2].Add(button); //add new item to the button list
    }

    public void RemoveItem(string name)
    {
        var localizedItem = LocalizationManager.Instance.GetItemsLocalizedValue(name);

        var itemToRemove = m_ButtonsList[2].FirstOrDefault(x => x.name == localizedItem); //search item to delete

        if (itemToRemove != null) //if item was successfuly found
        {
            m_ButtonsList[2].Remove(itemToRemove); //remove item from the buttons
            Destroy(itemToRemove.gameObject); //remove item from the inventory
        }
    }

    #endregion

    #region decoration methods

    private string GetDate()
    {
        var currentDate = DateTime.Today;

        return currentDate.Day + " <color=yellow>" + currentDate.ToString("MMM") + "</color> " + (currentDate.Year + 400);
    }

    private void SetBatteryStatus()
    {
        var batteryValue = UnityEngine.Random.Range(0.1f, 0.9f);

        m_BatteryImage.fillAmount = batteryValue;
        m_BatteryImage.color = ChangeBatteryColor(batteryValue);

        m_BatteryText.text = (batteryValue * 100).ToString("0") + "%";
    }

    private Color ChangeBatteryColor(float value)
    {
        var color = Color.green;

        if (value < .5f & value > .3f)
        {
            color = Color.yellow;
        }
        else if (value < .3f)
        {
            color = Color.red;
        }

        return color;
    }

    #region test battery methods

    [ContextMenu("SetLowBattery")]
    public void LowBattery()
    {
        var batteryValue = 0.2f;

        m_BatteryImage.fillAmount = batteryValue;
        m_BatteryImage.color = ChangeBatteryColor(batteryValue);

        m_BatteryText.text = (batteryValue * 100).ToString("0") + "%";
    }

    [ContextMenu("SetMiddleBattery")]
    public void MiddleBattery()
    {
        var batteryValue = 0.4f;

        m_BatteryImage.fillAmount = batteryValue;
        m_BatteryImage.color = ChangeBatteryColor(batteryValue);

        m_BatteryText.text = (batteryValue * 100).ToString("0") + "%";
    }

    #endregion

    #endregion
}