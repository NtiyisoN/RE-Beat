﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RangeEnemy : MonoBehaviour {

    public MageEnemyStats EnemyStats;

    [SerializeField] private Canvas UI;

    private EnemyMovement m_EnemyMovement; private Animator m_Animator;
    private TextMeshProUGUI m_Text;
    private Image m_AlarmImage;
    public bool m_IsPlayerInSight = false;
    public bool m_CanCreateNewFireball = true;

    // Use this for initialization
    void Start()
    {
        InitializeStats();

        InitializeEnemyMovement();

        InitializeAnimator();

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

    private void InitializeAnimator()
    {
        m_Animator = GetComponent<Animator>();

        if (m_Animator == null)
            Debug.LogError("RangeEnemy.InitializeAnimator: Can't find animator on GameObject");
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

    //simplify
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            /*m_IsPlayerInSight = false;
            m_EnemyMovement.isWaiting = false;
            EnableWarningSign(false);*/

            if (!m_IsPlayerInSight)
            {
                m_EnemyMovement.TurnAround();
            }

            collision.transform.GetComponent<Player>().playerStats.TakeDamage(EnemyStats.DamageAmount);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & !m_IsPlayerInSight)
        {
            m_IsPlayerInSight = true;
        }
    }

    private IEnumerator OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") & m_IsPlayerInSight)
        {
            m_IsPlayerInSight = false;

            yield return ResetState();
        }
    }

    private void Update()
    {
        if (GameMaster.Instance.isPlayerDead)
        {
            m_IsPlayerInSight = false;
            m_EnemyMovement.isWaiting = false;
            EnableWarningSign(false);
        }

        if (m_IsPlayerInSight)
        {
            m_EnemyMovement.isWaiting = true;
            EnableWarningSign(m_EnemyMovement.isWaiting);

            if (m_CanCreateNewFireball)
            {
                StartCoroutine(StartCast());
            }
        }
    }

    private IEnumerator StartCast()
    {
        if(m_IsPlayerInSight)
        {
            if (m_CanCreateNewFireball)
            {
                AudioManager.Instance.Play("Cast");

                Animate(true);

                m_CanCreateNewFireball = false;

                yield return new WaitForSeconds(0.6f);

                Animate(false);

                CreateFireball();

                yield return CastCooldown();
            }
        }
    }

    private void CreateFireball()
    {
        var newFireball = Resources.Load("Fireball");

        var instantiateFireball = Instantiate(newFireball, transform.position, transform.rotation) as GameObject;

        if (m_EnemyMovement.m_PosX < 0)
        {
            instantiateFireball.GetComponent<Fireball>().Direction = -Vector3.right;
        }
    }

    private IEnumerator CastCooldown()
    {
        yield return new WaitForSeconds(EnemyStats.AttackSpeed);

        m_CanCreateNewFireball = true;
    }


    private IEnumerator ResetState()
    {
        yield return new WaitForSeconds(1f);

        if (!m_IsPlayerInSight)
        {
            m_EnemyMovement.isWaiting = false;
            EnableWarningSign(false);
        }
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
            m_AlarmImage.gameObject.SetActive(isAttacking);
        }
    }

}
