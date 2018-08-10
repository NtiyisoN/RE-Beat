﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskUpdater : MonoBehaviour {

    [SerializeField] private string Name;
    [SerializeField, TextArea(2, 20)] private string UpdateText;
    [SerializeField] private bool DestroyEntireObject;

    private bool m_IsPlayerNear;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & !m_IsPlayerNear)
        {
            UpdateTask();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player") & !m_IsPlayerNear)
        {
            UpdateTask();
        }
    }

    private void UpdateTask()
    {
        if (CheckTask())
        {
            if (JournalManager.Instance.UpdateTask(Name, UpdateText))
            {
                m_IsPlayerNear = false;
                GameMaster.Instance.SaveTaskState(name);
                DestroyTaskUpdater();
            }
        }
    }

    public void DestroyTaskUpdater()
    {
        if (DestroyEntireObject)
            Destroy(gameObject);
        else
            Destroy(this);
    }

    private bool CheckTask()
    {
        if (string.IsNullOrEmpty(Name) | string.IsNullOrEmpty(UpdateText))
        {
            Debug.LogError("TaskGiver.CheckTask: Name and/or Update text - empty");
            return false;
        }

        return true;
    }
}
