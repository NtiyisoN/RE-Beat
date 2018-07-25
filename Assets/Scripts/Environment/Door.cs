﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour {

    [SerializeField] private Item KeyName;

    private SpriteRenderer m_InteractionButton;
    private bool m_IsPlayerNearDoor = false;

    private void Start()
    {
        InitializeInteractionButton();
        ShowInteractionKey(false);
    }

    private void InitializeInteractionButton()
    {
        m_InteractionButton = transform.GetChild(0).GetComponent<SpriteRenderer>();

        if (m_InteractionButton == null)
        {
            Debug.LogError("PickUpItem.InitializeInteractionButton: Can't find SpriteRenderer in child");
        }
    }

    private void Update()
    {
        if (m_IsPlayerNearDoor)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenDoor();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & !m_IsPlayerNearDoor)
        {
            m_IsPlayerNearDoor = true;
            ShowInteractionKey(m_IsPlayerNearDoor);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & m_IsPlayerNearDoor)
        {
            m_IsPlayerNearDoor = false;
            ShowInteractionKey(m_IsPlayerNearDoor);
        }
    }

    private void ShowInteractionKey(bool show)
    {
        if (m_InteractionButton != null)
            m_InteractionButton.gameObject.SetActive(show);
        else
            Debug.LogError("Door.ShowInteractionKey: InteractionButtonImage is not initialized");
    }

    private void OpenDoor()
    {
        if (string.IsNullOrEmpty(KeyName.Name))
        {
            Destroy(gameObject);
        }
        else if (PlayerStats.PlayerInventory.IsInBag(KeyName.Name))
        {
            PlayerStats.PlayerInventory.Remove(KeyName);
            Destroy(gameObject);
        }
        else
        {
            AnnouncerManager.Instance.DisplayAnnouncerMessage(GetMessage());
        }
    }

    private AnnouncerManager.Message GetMessage()
    {
        return new AnnouncerManager.Message(KeyName.Name + " required");
    }

}