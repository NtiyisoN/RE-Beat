﻿using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets._2D;

public class Trader : MonoBehaviour {
    
    [SerializeField] private GameObject m_StoreUI; //items that this vendor sells
    [SerializeField] private GameObject m_InventoryUI; //trader's store UI

    [Header("Description UI")]
    [SerializeField] private TextMeshProUGUI m_DescriptionNameText; //item name
    [SerializeField] private TextMeshProUGUI m_DescriptionText; //item description

    [Header("Scroll")]
    [SerializeField] private RectTransform m_Content; //traders store inventory
    [SerializeField] private GameObject m_ArrowUP; //arrow up image
    [SerializeField] private GameObject m_ArrowDown; //arrow down image

    [Header("Additional")]
    [SerializeField] private InteractionUIButton m_InteractionUIButton;

    private float m_DefaultYContentPosition; //default y position for trader's inventory

    //current selected item info
    private Item m_CurrentSelectedItem; 
    private GameObject m_CurrentSelectedItemGO;

    private bool m_IsPlayerNear; //indicates is player near
    private bool m_IsBoughtItem; //indicates that player bought item

    private int m_CurrentSelectedItemIndex = 0; //save current selected item to evaluate with next item's index

    private void Awake()
    {
        m_DefaultYContentPosition = m_Content.localPosition.y; //get default position y position for trader's inventory

        m_InteractionUIButton.PressInteractionButton = ShowUI;
    }

    // Update is called once per frame
    void Update () {

        if (m_IsPlayerNear) //if player is near
        {
            if (InputControlManager.Instance.IsBackMenuPressed() || InputControlManager.Instance.IsBackPressed()) //if back button button pressed
            {
                //if trader's store is open
                if (m_StoreUI.activeSelf)
                {
                    StartCoroutine(HideUI()); //hide npc ui
                    m_InteractionUIButton.SetActive(true);
                }
            }

            //if there is not selected object in trader's inventory
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                //select first item
                EventSystem.current.SetSelectedGameObject(m_InventoryUI.transform.GetChild(m_CurrentSelectedItemIndex).gameObject);
            }

            if (m_StoreUI.activeSelf) //if ui is open
            {
                if (InputControlManager.Instance.IsSubmitReleased() && m_CurrentSelectedItem != null)
                {
                    m_CurrentSelectedItemGO.GetComponent<TraderItem>().m_BuyingImage.fillAmount = 0f;
                    m_IsBoughtItem = false;
                }
                //submit button pressed to buy item
                else if (InputControlManager.Instance.IsSubmitPressing() && m_CurrentSelectedItem != null)
                {
                    if (m_CurrentSelectedItemGO.GetComponent<TraderItem>().m_BuyingImage.fillAmount >= 1f)
                    {
                        m_CurrentSelectedItemGO.GetComponent<TraderItem>().m_BuyingImage.fillAmount = 0f;

                        BuyItem();

                        m_IsBoughtItem = true;
                    }
                    else if (!m_IsBoughtItem)
                    {
                        InputControlManager.Instance.StartGamepadVibration(0.5f, 0.01f);
                        m_CurrentSelectedItemGO.GetComponent<TraderItem>().m_BuyingImage.fillAmount += (1f * Time.unscaledDeltaTime);
                    }
                }
            }
        }
        else if (m_InteractionUIButton.ActiveSelf() || m_StoreUI.activeSelf) //if interaction button or store ui active (when player is not near)
        {
            StartCoroutine( HideUI() ); //hide ui
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) //if player is near
        {
            m_IsPlayerNear = true; //indicates that plyear is near

            m_InteractionUIButton.SetIsPlayerNear(true);
            m_InteractionUIButton.SetActive(true); //show interaction elements
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) //if player leave trader trigger
        {
            m_IsPlayerNear = false; //indicate that player is not near trader

            m_InteractionUIButton.SetIsPlayerNear(false);
            StartCoroutine( HideUI() ); //hide npc ui
        }
    }

    private void ShowUI()
    {
        if (!m_StoreUI.activeSelf && m_IsPlayerNear)
        {
            PauseMenuManager.Instance.SetIsCantOpenPauseMenu(true); //don't allow to open pause menu

            m_StoreUI.SetActive(true); //show store ui
            m_InteractionUIButton.SetActive(false); //hide interaction elements

            Time.timeScale = 0f; //stop time

            EventSystem.current.SetSelectedGameObject(null); //remove selected gameobject to select first item in the list
            EventSystem.current.SetSelectedGameObject(m_InventoryUI.transform.GetChild(m_CurrentSelectedItemIndex).gameObject);

            GameMaster.Instance.m_Player.transform.GetChild(0).GetComponent<Platformer2DUserControl>().IsCanJump = false;
            GameMaster.Instance.m_Player.transform.GetChild(0).GetComponent<Player>().TriggerPlayerBussy(true);
        }
    }

    //hide all trader's ui elemetnts
    private IEnumerator HideUI()
    {
        Time.timeScale = 1f; //return time back to normal

        //remove buying fill
        if (m_CurrentSelectedItemGO != null)
        {
            m_CurrentSelectedItemGO.GetComponent<TraderItem>().m_BuyingImage.fillAmount = 0f;
            m_IsBoughtItem = false;
        }

        m_InteractionUIButton?.SetActive(false); //hide interaction UI

        m_StoreUI?.SetActive(false); //hide store UI

        if (GameMaster.Instance.m_Player != null)
            GameMaster.Instance.m_Player.transform.GetChild(0).GetComponent<Player>().TriggerPlayerBussy(false);

        //remove item's selection
        m_CurrentSelectedItemIndex = 0;

        EventSystem.current.SetSelectedGameObject(null); //remove selected gameobject to select first item in the list
        EventSystem.current.SetSelectedGameObject(m_InventoryUI.transform.GetChild(m_CurrentSelectedItemIndex).gameObject);

        yield return new WaitForEndOfFrame();

        GameMaster.Instance.m_Player.transform.GetChild(0).GetComponent<Platformer2DUserControl>().IsCanJump = true;

        PauseMenuManager.Instance.SetIsCantOpenPauseMenu(false); //allow to open pause menu
    }

    public void ShowItemDescription(GameObject itemToDisplayGO)
    {
        m_CurrentSelectedItem = itemToDisplayGO.GetComponent<TraderItem>().m_TraderItem; //get selected item description
        m_CurrentSelectedItemGO = itemToDisplayGO; //store item gameobject

        //move rect depend on items count
        var index = itemToDisplayGO.transform.GetSiblingIndex();

        //play arrows animation if arrows are available
        if (m_ArrowUP.activeSelf) 
            ManageArrowsVisibility(index, m_CurrentSelectedItemIndex);

        m_CurrentSelectedItemIndex = index;

        //if selected item index 4 or grater - move rect
        if (index > 1)
            m_Content.localPosition = m_Content.localPosition.
                With(y: m_DefaultYContentPosition + .5f * (index - 1)); //.5f * index more than 3 (.5f * 2)

        else //if selected item index is less than 4 return rect to normal position
            m_Content.localPosition = m_Content.localPosition.
                With(y: m_DefaultYContentPosition);

        //itemToDisplayGO.transform.GetSiblingIndex()

        //set text to display on description ui
        m_DescriptionNameText.text = LocalizationManager.Instance.GetItemsLocalizedValue(m_CurrentSelectedItem.itemDescription.Name);

        m_DescriptionText.text = LocalizationManager.Instance.GetItemsLocalizedValue(m_CurrentSelectedItem.itemDescription.Description);
    }

    #region arrows control

    //manage arraows visibility (base on current selected item index)
    public void ManageArrowsVisibility(int index, int previousIndex)
    {
        var epsilon = .01f; //epsilon to check alpha value

        if (index > 0) //index below first items
        {
            if (index + 1 == m_Content.childCount) //there is more items below current item
            {
                //show ArrowUp and hide ArrowDown
                ArrowHideAnimation(m_ArrowUP, m_ArrowDown, epsilon);
            }
            //selected item is below previous selected
            else if (index > m_CurrentSelectedItemIndex)
            {
                //show arow up and play arrow down move animation
                ArrowMoveAnimation(m_ArrowUP, m_ArrowDown, epsilon);
            }
            //selected item is above previous selected
            else
            {
                //show arrow down and play arrow up move animation
                ArrowMoveAnimation(m_ArrowDown, m_ArrowUP, epsilon);
            }
        }
        else //index on first items
        {
            //there is only 1 item in list
            if (m_Content.childCount == 1)
            {
                //hide buttons
                ArrowVisibility(false, false);
            }
            else
            {
                //show arrow down and hide arrow up
                ArrowHideAnimation(m_ArrowDown, m_ArrowUP, epsilon);
            }
        }
    }

    private void ArrowHideAnimation(GameObject toShow, GameObject toHide, float epsilon)
    {
        //if there is more than 1 item in list and toShow arrow is hiiden
        if (m_Content.childCount > 1 && toShow.GetComponent<Image>().color.a < epsilon)
            //play appear animation
            toShow.GetComponent<Animator>().SetTrigger("Show");

        //play hide animation
        toHide.GetComponent<Animator>().SetTrigger("Hide");
    }

    private void ArrowMoveAnimation(GameObject toShow, GameObject toMove, float epsilon)
    {
        //if toShow arrow is hidden
        if (toShow.GetComponent<Image>().color.a < epsilon)
            //play appear animation
            toShow.GetComponent<Animator>().SetTrigger("Show");

        //play move down animation
        toMove.GetComponent<Animator>().SetTrigger("Move");
    }

    //activate/disable arrows base on values
    private void ArrowVisibility(bool upValue, bool downValue)
    {
        m_ArrowUP.SetActive(upValue);
        m_ArrowDown.SetActive(downValue);
    }

    #endregion

    public void BuyItem()
    {
        if (m_CurrentSelectedItem != null) //if item were selected
        {
            //current player on scene
            var player = GameMaster.Instance.m_Player.transform.GetChild(0).GetComponent<Player>().playerStats;

            if (player != null)
            {
                if ((PlayerStats.Scrap - m_CurrentSelectedItem.itemDescription.ScrapAmount) >= 0) //if player has enough scrap
                {
                    if (player.CurrentHealth == player.MaxHealth &
                        m_CurrentSelectedItem.itemDescription.itemType == ItemDescription.ItemType.Heal)
                    {
                        UIManager.Instance.DisplayNotificationMessage("Can't buy repair at max health!",
                            UIManager.Message.MessageType.Message);
                    }
                    else
                    {
                        PlayerStats.Scrap = -m_CurrentSelectedItem.itemDescription.ScrapAmount; //change player's scrap amount

                        //add item to the inventory if it's not heal potion
                        if (m_CurrentSelectedItem.itemDescription.itemType != ItemDescription.ItemType.Heal)
                        {
                            //get item info
                            var itemName = LocalizationManager.Instance.GetItemsLocalizedValue(m_CurrentSelectedItem.itemDescription.Name);
                            var inventoryMessage = LocalizationManager.Instance.GetItemsLocalizedValue("add_to_inventory_message");

                            //display that item was added to inventory
                            UIManager.Instance.DisplayNotificationMessage(itemName + " " + inventoryMessage,
                                UIManager.Message.MessageType.Item);

                            //add item to inventory
                            PlayerStats.PlayerInventory.Add(m_CurrentSelectedItem.itemDescription, m_CurrentSelectedItem.Image.name);
                        }

                        //apply item upgrade to the player
                        m_CurrentSelectedItemGO.GetComponent<TraderItem>().ApplyUpgrade(player);

                        //move item from store ui (so childCount works properly)
                        if (!m_CurrentSelectedItemGO.GetComponent<TraderItem>().m_IsInfiniteAmount)
                        {
                            m_CurrentSelectedItemGO.transform.SetParent(null);
                            Destroy(m_CurrentSelectedItemGO);

                            //get in focuse next trader's item
                            EventSystem.current.SetSelectedGameObject(null);
                        }
                    }
                }
                else //if player does not have enought scrap
                {
                    UIManager.Instance.DisplayNotificationMessage("Payment rejected <br> \"You don't have enough scraps\"",
                                                     UIManager.Message.MessageType.Message);
                }
            }
        }
    }

    private bool CheckInventoryAmount()
    {
        return m_StoreUI.transform.GetChild(0).transform.childCount > 0; //if there are items to sell
    }

}
