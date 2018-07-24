﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RangeEnemy : MonoBehaviour {

    public MageEnemyStats EnemyStats;

    private EnemyMovement m_EnemyMovement;
    private bool m_IsPlayerDead = false;
    private bool m_CanCreateNewFireball = true;
    private Animator m_Animator;
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private GameObject m_AlarmImage;

    // Use this for initialization
    void Start()
    {
        InitializeStats();

        InitializeEnemyMovement();

        InitializeAnimator();
    }

    private void InitializeStats()
    {
        EnemyStats.Initialize(gameObject);
    }

    private void InitializeEnemyMovement()
    {
        m_EnemyMovement = GetComponent<EnemyMovement>();
    }

    private void InitializeAnimator()
    {
        m_Animator = GetComponent<Animator>();

        if (m_Animator == null)
            Debug.LogError("RangeEnemy.InitializeAnimator: Can't find animator on GameObject");
    }

    private void Update()
    {
        if (GameMaster.Instance.isPlayerDead)
        {
            ResetState();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            KillPlayer(collision);
        }
    }

    private IEnumerator OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & !m_IsPlayerDead)
        {
            yield return StartCast();
        }
    }

    private IEnumerator OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & !m_IsPlayerDead & m_CanCreateNewFireball)
        {
            yield return StartCast();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & !m_IsPlayerDead)
        {
            ResetState();
        }
    }

    private void KillPlayer(Collision2D collision)
    {
        m_IsPlayerDead = true;

        m_EnemyMovement.isWaiting = false;
        EnableWarningSign(false);

        collision.transform.GetComponent<Player>().playerStats.TakeDamage(999);
    }

    private IEnumerator StartCast()
    {
        if (m_CanCreateNewFireball)
        {
            m_EnemyMovement.isWaiting = true;
            Animate(true);
            EnableWarningSign(true);

            m_CanCreateNewFireball = false;

            yield return new WaitForSeconds(0.6f);

            Animate(false);

            CreateFireball();

            yield return new WaitForSeconds(EnemyStats.AttackSpeed);

            m_CanCreateNewFireball = true;
        }
    }

    private void CreateFireball()
    {
        var newFireball = Resources.Load("Fireball");
        var posX = m_Animator.GetFloat("PosX");

        var instantiateFireball = Instantiate(newFireball, transform.position, transform.rotation) as GameObject;

        if (posX < 0)
            instantiateFireball.GetComponent<Fireball>().Direction = -Vector3.right;
    }

    private void Animate(bool isAttacking)
    {
        if (m_Animator != null)
        {
            m_Animator.SetBool("isAttacking", isAttacking);    
        }
    }

    private void EnableWarningSign(bool isAttacking)
    {
        if (m_AlarmImage != null)
        {
            m_AlarmImage.SetActive(isAttacking);
        }
    }

    private void ResetState()
    {
        m_IsPlayerDead = false;
        m_EnemyMovement.isWaiting = false;
        EnableWarningSign(false);
    }

}