﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour {

    private Transform m_Flame;

    private void Start()
    {
        InitializeFlame();
    }

    private void InitializeFlame()
    {
        m_Flame = gameObject.transform.GetChild(0);

        if (m_Flame == null)
        {
            Debug.LogError("RespawnPoint: Can't find flame in child");
        }

        SetActiveFlame(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameMaster.Instance.ChangeRespawnPoint(gameObject.transform);

            if (!m_Flame.gameObject.activeSelf)
                SetActiveFlame(true);
        }
    }

    public void SetActiveFlame(bool value)
    {
        m_Flame.gameObject.SetActive(value);
    }
}