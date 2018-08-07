﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyMovement : MonoBehaviour {

    private Rigidbody2D m_Rigidbody2D;
    private Animator m_Animator;
    private Vector2 m_PreviousPosition = Vector2.zero;
    private bool m_IsWaiting = false;
    private float m_Speed;

    [SerializeField] private float IdleTime = 2f;

    [HideInInspector] public float m_PosX = -1f;

    public delegate void VoidDelegate(bool value);
    public event VoidDelegate OnWaitingStateChange;

    // Use this for initialization
    void Start () {

        InitializeRigidBody();

        InitializeAnimator();

        SubscribeOnEvents();

        SpeedChange(GetDefaultSpeed());
    }

    #region Initialize
    private void InitializeRigidBody()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();

        if (m_Rigidbody2D == null)
        {
            Debug.LogError("EnemyMovement.InitializeRigidBody: Can't find rigidbody on gameobject");
        }
    }

    private void InitializeAnimator()
    {
        m_Animator = GetComponent<Animator>();

        if (m_Animator == null)
        {
            Debug.LogError("EnemyMovement.InitializeAnimator: Can't find animator on gameobject");
        }
    }

    private void SubscribeOnEvents()
    {
        if (GetComponent<PatrolEnemy>() != null)
        {
            var patrolEnemy = GetComponent<PatrolEnemy>();

            patrolEnemy.OnPlayerSpot += ChangeWaitingState;
            patrolEnemy.EnemyStats.OnSpeedChange += SpeedChange;
            patrolEnemy.EnemyStats.OnEnemyTakeDamage += isPlayerNear =>
            {
                if (!isPlayerNear)
                    TurnAround();
            };
        }
        else
        {
            var rangeEnemy = GetComponent<RangeEnemy>();

            rangeEnemy.OnPlayerSpot += ChangeWaitingState;
            rangeEnemy.EnemyStats.OnSpeedChange += SpeedChange;
            rangeEnemy.EnemyStats.OnEnemyTakeDamage += isPlayerNear =>
            {
                if (!isPlayerNear)
                    TurnAround();
            };
        }
    }

    private float GetDefaultSpeed()
    {
        var defaultSpeed = 0f;

        if (GetComponent<PatrolEnemy>() != null)
        {
            defaultSpeed = GetComponent<PatrolEnemy>().EnemyStats.Speed;
        }
        else
        {
            defaultSpeed = GetComponent<RangeEnemy>().EnemyStats.Speed;
        }

        return defaultSpeed;
    }

    #endregion

    private void FixedUpdate()
    {
        if (!m_IsWaiting)
        {
            if (!m_Animator.GetBool("isWalking"))
                SetAnimation();

            m_Rigidbody2D.position += new Vector2(m_PosX, 0) * Time.fixedDeltaTime * m_Speed;
            SetAnimation();

            if (m_Rigidbody2D.position == m_PreviousPosition & !m_IsWaiting)
            {
                StartCoroutine(Idle());
            }
            else
                m_PreviousPosition = m_Rigidbody2D.position;
        }
        else if (m_Animator.GetBool("isWalking"))
        {
            SetAnimation();
        }
    }

    private void SpeedChange(float speed)
    {
        m_Speed = speed;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            StartCoroutine(Idle());
        }
    }

    private IEnumerator Idle()
    {
        if (!m_IsWaiting)
        {
            ChangeWaitingState(true);

            TurnAround();
            SetAnimation();

            yield return new WaitForSeconds(IdleTime);

            ChangeWaitingState(false);
        }
    }

    public void TurnAround()
    {
        transform.localScale = new Vector3(m_PosX, 1, 1);
        m_PosX = -m_PosX;
    }

    private void SetAnimation()
    {
        m_Animator.SetBool("isWalking", !m_IsWaiting);
    }

    private void ChangeWaitingState(bool value)
    {
        m_IsWaiting = value;

        if (OnWaitingStateChange != null)
            OnWaitingStateChange(value);
    }
}
