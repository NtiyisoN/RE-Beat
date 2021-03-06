﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour {

    #region message class

    public class Message
    {
        public enum MessageType { Item, Scene, Task, Message }

        public string message = "Empty message"; //message to display
        public float duration; //time to display

        public Color color;
        public MessageType messageType;

        public Message(string message, MessageType messageType, float duration = 3f)
        {
            this.message = message;
            this.duration = duration;
            this.messageType = messageType;

            SetColor(this.messageType);
        }

        private void SetColor(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Item:
                    ColorUtility.TryParseHtmlString("#FF0800", out color);
                    break;

                case MessageType.Message:
                    ColorUtility.TryParseHtmlString("#FF7500", out color);
                    break;

                case MessageType.Scene:
                    ColorUtility.TryParseHtmlString("#DE00FF", out color);
                    break;

                case MessageType.Task:
                    ColorUtility.TryParseHtmlString("#FF0069", out color);
                    break;
            }

            color = color.ChangeColor(a: 0.8f);
        }

        public void PlayNotificationSound()
        {
            AudioManager.Instance.Play("Noti" + this.messageType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            var compareItem = obj as Message;

            if (ReferenceEquals(compareItem, null))
                return false;
            else
            {
                return this.message.CompareTo(compareItem.message) == 0;
            }
        }

        public override int GetHashCode()
        {
            return message.GetHashCode();
        }
    }

    #endregion

    #region Singleton
    public static UIManager Instance;

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
            m_MessagePipeline = new Queue<Message>(); //initialize pipeline

            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    #endregion

    #region private fields

    #region serialize fields

    [Header("Effect")]
    [SerializeField] private Image m_BackgroundImage;

    [Header("Upper")]
    [SerializeField] private GameObject m_LifePanel; //life panel
    [SerializeField] private Image m_BulletImage; //bullet display cooldown
    [SerializeField] private Image m_FallAttack; //fall attack display cooldown

    [Header("Scrap")]
    [SerializeField] private TextMeshProUGUI m_AmountText; //current coins amount
    [SerializeField] private TextMeshProUGUI m_AddScrapText; //coins to add
    [SerializeField] private TextMeshProUGUI m_AmountTextShadow; //amount text shadow
    [SerializeField] private TextMeshProUGUI m_AddScrapTextShadow; //add scrap tex shadow

    [Header("Notification")]
    [SerializeField] private GameObject m_NotificationUI; //notification ui
    [SerializeField] private TextMeshProUGUI m_Text; //notification text
    [SerializeField] private Animator m_Animator; //notification animator

    [Header("Revive")]
    [SerializeField] private GameObject m_RevivePanel;

    #endregion

    private List<GameObject> m_HealthInPanel = new List<GameObject>(); //
    private int m_CurrentActiveHPIndex = 0;

    //notification fields
    private Queue<Message> m_MessagePipeline; //display message pipeline
    private bool m_isShowingPipeline = false; //is currently showing pipeline

    #endregion

    #region initialize

    private void Start()
    {
        InitializeHealList();

        PlayerStats.OnScrapAmountChange += ChangeScrapAmount; //subscribe on coins amount change

        m_AmountText.text = m_AmountTextShadow.text = PlayerStats.Scrap.ToString(); //display current scrap amount

        if (PlayerStats.m_IsFallAttack)
            SetFallAttackImageActive();
    }

    private void OnDestroy()
    {
        PlayerStats.OnScrapAmountChange -= ChangeScrapAmount;
    }

    #endregion

    #region notification 

    private IEnumerator DisplayMessage()
    {
        if (!m_NotificationUI.activeSelf) //play appear animation
            yield return SetActiveNotificationUI(true);

        var itemToDisplay = m_MessagePipeline.Peek(); //get firs item in Queue (peek instead of Dequeue(), to left this message in pipeline so contains method will work properly)

        itemToDisplay.PlayNotificationSound(); //play notification sound

        m_NotificationUI.GetComponent<Image>().color = itemToDisplay.color; //change notification color
        m_Text.text = itemToDisplay.message; //change notification text

        yield return new WaitForSecondsRealtime(itemToDisplay.duration); //display need amount time

        m_MessagePipeline.Dequeue();

        if (m_MessagePipeline.Count != 0) //if there is items in queue
        {
            //display next notification
            yield return SetActiveNotificationUI(false);
            yield return DisplayMessage();
        }
        else //if there is not items in queue
        {
            m_isShowingPipeline = false;
            yield return SetActiveNotificationUI(false);
        }
    }

    private IEnumerator SetActiveNotificationUI(bool active)
    {
        if (!active && !m_isShowingPipeline)
        {
            m_Text.gameObject.SetActive(false);

            m_Animator.SetTrigger("Disappear");

            yield return new WaitForEndOfFrame();

            yield return new WaitForSecondsRealtime(m_Animator.GetCurrentAnimatorStateInfo(0).length);
        }

        m_NotificationUI.SetActive(active);
    }


    public void DisplayNotificationMessage(string messageText, Message.MessageType messageType, float duration = 3f)
    {
        var message = new Message(messageText, messageType, duration);

        if (!m_MessagePipeline.Contains( message ))
        {
            m_MessagePipeline.Enqueue(message);

            if (!m_isShowingPipeline)
            {
                m_isShowingPipeline = true;
                StartCoroutine(DisplayMessage());
            }
        }
    }

    #endregion

    #region scrap

    public void ChangeScrapAmount(int value)
    {
        StartCoroutine(DisplayChangeAmount(value));
    }

    private IEnumerator DisplayChangeAmount(int value)
    {
        var currentCoinsCount = Convert.ToInt32( m_AmountText.text ); //current scrap amount displayed

        var sign = value > 0 ? '+' : '-'; //draw add amoun sign
        var addValue = value > 0 ? 1 : -1; //add value for loop

        value = Mathf.Abs(value);

        m_AddScrapTextShadow.gameObject.SetActive(true); //display add amount text
        m_AddScrapText.text = m_AddScrapTextShadow.text = sign + value.ToString(); //display amount that will be added

        yield return new WaitForSecondsRealtime(.5f); //time before start add amount

        //display add animation
        for (; value > 0; value--)
        {
            currentCoinsCount += addValue;
            m_AmountText.text = m_AmountTextShadow.text = currentCoinsCount.ToString();

            m_AddScrapText.text = m_AddScrapTextShadow.text = sign + value.ToString();

            yield return new WaitForSecondsRealtime(.005f);
        }

        m_AddScrapText.text = m_AddScrapTextShadow.text = sign + value.ToString(); //show zero add value at the end

        yield return new WaitForSecondsRealtime(.5f); //wait before hide add text

        m_AddScrapTextShadow.gameObject.SetActive(false);
    }

    #endregion

    #region upper panel

    private void InitializeHealList()
    {
        for (var index = 0; index < m_LifePanel.transform.childCount; index++)
        {
            var lifeGameObject = m_LifePanel.transform.GetChild(index).gameObject;
            lifeGameObject.GetComponent<Animator>().SetFloat("AppearTime", UnityEngine.Random.Range(.6f, 1.6f));
            m_HealthInPanel.Add(lifeGameObject);
        }

        m_CurrentActiveHPIndex = m_HealthInPanel.Count - 1;
    }

    #region public methods

    public void AddHealth()
    {
        if (m_CurrentActiveHPIndex <= 5)
        {
            m_HealthInPanel[m_CurrentActiveHPIndex].GetComponent<Animator>().SetBool("Disable", false);

            if (m_CurrentActiveHPIndex < 5)
                m_CurrentActiveHPIndex++;

        }
    }

    public void RemoveHealth()
    {
        if (m_CurrentActiveHPIndex >= 0)
        {
            m_HealthInPanel[m_CurrentActiveHPIndex].GetComponent<Animator>().SetBool("Disable", true);

            if (m_CurrentActiveHPIndex > 0)
                m_CurrentActiveHPIndex--;
        }
    }

    public void ResetState()
    {
        for (var index = 0; index < m_HealthInPanel.Count; index++)
        {
            m_HealthInPanel[index].GetComponent<Animator>().SetBool("Disable", false);
        }

        m_CurrentActiveHPIndex = m_HealthInPanel.Count - 1;
    }

    #region skills

    public void ShowFallAttack(bool value)
    {
        m_FallAttack.gameObject.transform.parent.gameObject.SetActive(value);
    }

    public void BulletCooldown(float cooldown)
    {
        StartCoroutine(DisplayCooldown(cooldown, m_BulletImage));
    }

    public void FallAttackCooldown(float cooldown)
    {
        StartCoroutine(DisplayCooldown(cooldown, m_FallAttack));
    }

    public void SetFallAttackImageActive()
    {
        m_FallAttack.gameObject.transform.parent.gameObject.SetActive(true);
    }

    private IEnumerator DisplayCooldown(float time, Image displayCooldown)
    {
        displayCooldown.fillAmount = 0f;

        var tickTime = time * .1f;

        while (displayCooldown.fillAmount < 1)
        {
            yield return new WaitForSeconds(tickTime);
            displayCooldown.fillAmount += .1f;
        }
    }

    #endregion

    #endregion

    #endregion

    #region revive panel

    public void AddRevive(int index)
    {
        if (m_RevivePanel.transform.childCount >= index && index > 0)
        {
            m_RevivePanel.transform.GetChild(index - 1).GetComponent<Animator>().SetBool("Disable", false);
        }
    }

    public void RemoveRevive(int index)
    {
        if (m_RevivePanel.transform.childCount >= index && index > 0)
        {
            m_RevivePanel.transform.GetChild(index - 1).GetComponent<Animator>().SetBool("Disable", true);
        }
    }

    #region test methods

    [ContextMenu("UserRevive2")]
    public void UserRevive2()
    {
        RemoveRevive(2);
    }

    [ContextMenu("UserRevive1")]
    public void UserRevive1()
    {
        RemoveRevive(1);
    }

    [ContextMenu("AddRevive1")]
    public void AddRevive1()
    {
        AddRevive(1);
    }

    [ContextMenu("AddRevive2")]
    public void AddRevive2()
    {
        AddRevive(2);
    }

    #endregion

    #endregion

    #region ui control

    public void EnableRegularUI()
    {
        m_BulletImage.transform.parent.gameObject.SetActive(true);
        m_AmountText.gameObject.SetActive(true);
        m_AmountTextShadow.gameObject.SetActive(true);
        m_AddScrapTextShadow.gameObject.SetActive(true);

        m_FallAttack.fillAmount = m_BulletImage.fillAmount = 1f;

        m_BackgroundImage.color = m_BackgroundImage.color.ChangeColor(.377f, .377f, .377f, m_BackgroundImage.color.a);
    }

    public void EnableCompanionUI()
    {
        m_AmountText.gameObject.SetActive(false);
        m_AmountTextShadow.gameObject.SetActive(false);
        m_AddScrapTextShadow.gameObject.SetActive(false);

        m_FallAttack.fillAmount = m_BulletImage.fillAmount = 1f;

        m_BackgroundImage.color = m_BackgroundImage.color.ChangeColor(.549f, .980f, .984f, m_BackgroundImage.color.a);
    }

    #endregion

    public void SetReviveAvailable(bool value)
    {
        m_RevivePanel.SetActive(value);
    }
}
