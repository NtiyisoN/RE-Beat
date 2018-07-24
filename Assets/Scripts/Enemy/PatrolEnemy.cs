﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyMovement))]
public class PatrolEnemy : MonoBehaviour {

    public Stats EnemyStats;

    private EnemyMovement m_EnemyMovement;
    private TextMeshProUGUI m_Text;
    private Image m_AlarmImage;
    private float m_UpdateTime = 0f;
    private bool m_IsPlayerDead;

    [SerializeField] private Canvas UI;
    [SerializeField] private string Phrase;
    [SerializeField] private float WaitTimeAfterSpot = 2f;

	// Use this for initialization
	void Start () {

        InitializeStats();

        InitializeEnemyMovement();

        InitializeEnemyUI();

        m_AlarmImage.gameObject.SetActive(false);
    }

    private void InitializeStats()
    {
        EnemyStats.Initialize(gameObject);
    }

    private void InitializeEnemyMovement()
    {
        m_EnemyMovement = GetComponent<EnemyMovement>();
    }

    private void InitializeEnemyUI()
    {
        if (UI != null)
        {
            m_Text = UI.GetComponentInChildren<TextMeshProUGUI>();

            if (m_Text == null)
                Debug.LogError("Can't initizlize text");

            m_AlarmImage = UI.GetComponentInChildren<Image>(); 

            if (m_AlarmImage == null)
                Debug.LogError("Can't initizlize image");
        }
    }

    private string ChangePhrase()
    {
        return Phrase.Replace("<br>", "\n");
    }

    private void Update()
    {
        if (m_EnemyMovement.isWaiting)
        {
            if (m_UpdateTime < Time.time)
            {
                m_UpdateTime = Time.time + 0.5f;

                if (m_AlarmImage.gameObject.activeSelf)
                    m_EnemyMovement.isWaiting = false;
            }
        }
    }

    private IEnumerator ShowPhrase()
    {
        m_Text.text = ChangePhrase();
        m_EnemyMovement.isWaiting = true;

        yield return new WaitForSeconds(WaitTimeAfterSpot);

        m_Text.text = string.Empty;
        m_EnemyMovement.isWaiting = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player") & !m_IsPlayerDead)
        {
            m_IsPlayerDead = true;
            PlayerSpot(false);
            AttackPlayer(collision);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (m_IsPlayerDead)
            m_IsPlayerDead = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerSpot(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerSpot(false);
        }
    }

    private void AttackPlayer(Collision2D collision)
    {
        collision.transform.GetComponent<Player>().playerStats.TakeDamage(999);
        StartCoroutine(ShowPhrase());
    }

    private void PlayerSpot(bool isSpot)
    {
        m_AlarmImage.gameObject.SetActive(isSpot);

        if (isSpot)
            m_EnemyMovement.Speed = 2f;
        else
            m_EnemyMovement.Speed = 1f;
    }
}
