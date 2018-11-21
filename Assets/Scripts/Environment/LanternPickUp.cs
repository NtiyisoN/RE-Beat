﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class LanternPickUp : MonoBehaviour {

    [SerializeField] private GameObject m_InteractionUIPrefab;

    private GameObject m_InteractionUI;
    private Transform m_Player;

    private Vector2 m_RespawnPoint;
    private bool m_IsQuitting; //is application closing

    // Use this for initialization
    void Start () {

        m_InteractionUI = Instantiate(m_InteractionUIPrefab, transform);

        m_RespawnPoint = transform.position;

        SubscribeToEvents();

        m_InteractionUI.SetActive(false);
	}

    private void SubscribeToEvents()
    {
        PauseMenuManager.Instance.OnReturnToStartSceen += ChangeIsQuitting; //if player is open start screen
        MoveToNextScene.IsMoveToNextScene += ChangeIsQuitting; //is player is move to the next sceen
    }

    private void OnApplicationQuit()
    {
        ChangeIsQuitting(true);
    }

    private void OnDestroy()
    {
        if (!m_IsQuitting)
        {
            SetPlayerCarriesLantern(false);
            
            Instantiate(Resources.Load("Items/LanternAppear"), m_RespawnPoint, Quaternion.identity);
        }

        PauseMenuManager.Instance.OnReturnToStartSceen -= ChangeIsQuitting; //if player is open start screen
        MoveToNextScene.IsMoveToNextScene -= ChangeIsQuitting; //is player is move to the next sceen
    }

    // Update is called once per frame
    void Update () {
		
        if (m_Player != null)
        {
            if (CrossPlatformInputManager.GetButtonDown("Submit"))
            {
                SetPlayerCarriesLantern(true);
            }
        }
        else if (transform.parent != null)
        {
            if (CrossPlatformInputManager.GetButtonDown("Submit"))
            {
                SetPlayerCarriesLantern(false);
                GameMaster.Instance.SaveState(transform.name, new ObjectPosition(transform.position), GameMaster.RecreateType.Position);
            }
        }

	}

    private void SetPlayerCarriesLantern(bool value)
    {
        if (value)
        {
            transform.SetParent(m_Player);
            transform.localPosition = new Vector2(0.3f, 0f);
        }
        else
        {
            transform.SetParent(null);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            SetPlayerNearLantern(true, collision.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            SetPlayerNearLantern(false, null);
        }
    }

    private void SetPlayerNearLantern(bool value, Transform player)
    {
        m_Player = player;
        m_InteractionUI.SetActive(value);
    }

    private void ChangeIsQuitting(bool value)
    {
        m_IsQuitting = value;
    }
}
