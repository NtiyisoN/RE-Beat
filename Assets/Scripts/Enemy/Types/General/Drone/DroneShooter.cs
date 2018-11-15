﻿using System.Collections;
using UnityEngine;
using Pathfinding;

/*[CustomEditor(typeof(DroneKamikaze)), CanEditMultipleObjects]
public class DroneScriptEditor : Editor
{
    public SerializedProperty
             drone_Type,
             whatIsEnemy_Prop,
             trailMaterial_Prop,
             bulletPrefab_Prop,
             deathParticles_Prop,
             detectionTrigger_Prop,
             shootTrigger_Prop,
             deathDetonationTimer_Prop,
             damageAmount_Prop,
             speed_Prop,
             updateRate_Prop,
             health_Prop,
             attackSpeed_Prop,
             patrolPoints_Prop;

    private void OnEnable()
    {
        // Setup the SerializedProperties
        drone_Type = serializedObject.FindProperty("m_DroneType");
        whatIsEnemy_Prop = serializedObject.FindProperty("WhatIsEnemy");
        trailMaterial_Prop = serializedObject.FindProperty("TrailMaterial");
        bulletPrefab_Prop = serializedObject.FindProperty("BulletTrailPrefab");
        deathParticles_Prop = serializedObject.FindProperty("DeathParticles");
        detectionTrigger_Prop = serializedObject.FindProperty("ChasingRange");
        deathDetonationTimer_Prop = serializedObject.FindProperty("DeathDetonationTimer");
        damageAmount_Prop = serializedObject.FindProperty("DamageAmount");
        speed_Prop = serializedObject.FindProperty("Speed");
        shootTrigger_Prop = serializedObject.FindProperty("ShootingRange");
        updateRate_Prop = serializedObject.FindProperty("UpdateRate");
        health_Prop = serializedObject.FindProperty("Health");
        attackSpeed_Prop = serializedObject.FindProperty("AttackSpeed");
        patrolPoints_Prop = serializedObject.FindProperty("m_PatrolPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(drone_Type);

        var droneType = (DroneKamikaze.DroneType)drone_Type.enumValueIndex;

        if (droneType == DroneKamikaze.DroneType.Shooter)
            InitializeShooterGUI();

        EditorGUILayout.ObjectField(deathParticles_Prop, new GUIContent("Death Particle"));
        EditorGUILayout.Slider(deathDetonationTimer_Prop, 0f, 10f, new GUIContent("Death Detonation Timer"));
        EditorGUILayout.IntSlider(damageAmount_Prop, 0, 10, new GUIContent("Damage Amount"));
        EditorGUILayout.IntSlider(health_Prop, 0, 10, new GUIContent("Health"));

        serializedObject.ApplyModifiedProperties();
    }
   

    private void InitializeShooterGUI()
    {
        EditorGUILayout.ObjectField(trailMaterial_Prop, new GUIContent("TrailMaterial"));
        EditorGUILayout.ObjectField(bulletPrefab_Prop, new GUIContent("BulletPrefab"));
        EditorGUILayout.ObjectField(detectionTrigger_Prop, new GUIContent("Chasing Range"));
        EditorGUILayout.ObjectField(shootTrigger_Prop, new GUIContent("Shooting Range"));
        EditorGUILayout.Slider(speed_Prop, 100f, 1000f, new GUIContent("Speed"));
        EditorGUILayout.Slider(updateRate_Prop, 1f, 10f, new GUIContent("Update Rate"));
        EditorGUILayout.Slider(attackSpeed_Prop, 1f, 5f, new GUIContent("Attack Speed"));
        EditorGUILayout.PropertyField(patrolPoints_Prop, true);
    }
}*/

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Seeker))]
public class DroneShooter : MonoBehaviour {

    [SerializeField] private Material TrailMaterial;
    [SerializeField] private PlayerInTrigger m_ChaseRange;
    [SerializeField, Range(1f, 10f)] private float UpdateRate = 2f; //next point update rate
    [SerializeField, Range(100f, 1000f)] private float Speed = 300f; //drone speed

    [Header("Movement points")]
    [SerializeField] private Transform[] m_PatrolPoints;

    [Header("Effects")]
    [SerializeField] private GameObject BulletTrailPrefab;
    [SerializeField] private Animator m_RadarAnimator;
    [SerializeField] private DroneTargetLine m_TargetLine;

    private Rigidbody2D m_Rigidbody;
    private Seeker m_Seeker;
    private Path m_Path;
    private Transform m_Target; //player
    private DroneStats m_Stats;

    private bool m_PathIsEnded = false; //path is reached
    private readonly float m_NextWaypointDistance = 0.2f; 
    private int m_CurrentWaypoint = 0;
    private int m_CurrentPatrolPoint = 0;

    private bool m_IsPlayerInShootingRange = false; //is drone chasing player
    private bool m_IsDestroying = false;
    private float m_AttackCooldownTimer = 0f;

    #region initialize

    // Use this for initialization
    private void Start () {

        InitializeComponents(); //initialize rigidbody and seeker

        m_ChaseRange.OnPlayerInTrigger += StartChase;

        StartCoroutine(PatrolBetweenPoints());
    }

    private void InitializeComponents()
    {
        m_Rigidbody = GetComponent<Rigidbody2D>();
        m_Seeker = GetComponent<Seeker>();
        m_Stats = GetComponent<DroneStats>();

        m_Stats.OnDroneDestroy += SetOnDestroy;
    }

    #endregion

    private void FixedUpdate()
    {
        //if player is not in shooting range, drone is not destroying and there is a path
        if (!m_IsPlayerInShootingRange & !m_IsDestroying & m_Path != null)
        {
            MoveInDirection(); //move drone
        }
    }

    private void Update()
    {
        if (m_Target != null) //if player in chase range
        {
            if (Vector2.Distance(transform.position, m_Target.position) < 3f) //if player in attack range
            {
                m_IsPlayerInShootingRange = true;
            }
            else //if player is not in attack range
            {
                m_IsPlayerInShootingRange = false;
            }

            //if player in attack range and drone is not destroying
            if (m_IsPlayerInShootingRange & !m_IsDestroying)
            {
                if (m_AttackCooldownTimer < Time.time) //drone can attack
                {
                    m_AttackCooldownTimer = Time.time + m_Stats.AttackSpeed + .3f; //next available attack time
                    StartCoroutine(Shoot()); //shoot at player
                }
            }
        }
        else if (!m_IsDestroying & m_IsPlayerInShootingRange) //if player is dead but drone still want to attack him
        {
            m_IsPlayerInShootingRange = false; //player is not in attack range

            StartCoroutine(PatrolBetweenPoints()); //continue patrolling
        }
        else if (m_RadarAnimator.GetBool("Threat") & !m_IsDestroying)
        {
            m_RadarAnimator.SetBool("Threat", false);
        }
    }

    private void StartChase(bool value, Transform target)
    {      
        m_Target = target;

        if (target != null)
            InitializeChasing();
    }

    #region shooting

    private IEnumerator Shoot()
    {
        if (m_Target != null)
        {
            var whereToShoot = new Vector3(m_Target.position.x, m_Target.position.y + 0.5f);

            var m_firePointPosition = transform.position;

            m_TargetLine.gameObject.SetActive(true);

            m_TargetLine.SetTarget(whereToShoot);

            yield return new WaitForSeconds(.3f); //wait before shoot

            m_TargetLine.gameObject.SetActive(false);

            DrawBulletTrailEffect(whereToShoot);
        }
    }

    private void DrawBulletTrailEffect(Vector3 whereToShoot)
    {
        Vector3 difference = whereToShoot - transform.position;
        difference.Normalize();

        float rotationZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        var bullet = Instantiate(BulletTrailPrefab, transform.position,
                    Quaternion.Euler(0f, 0f, rotationZ));

        bullet.GetComponent<MoveBullet>().DamageAmount = m_Stats.DamageAmount;
    }

    #endregion

    private void InitializeChasing()
    {
        //start a new path to the target
        if (m_RadarAnimator != null)
            m_RadarAnimator.SetBool("Threat", true);

        m_Seeker.StartPath(transform.position, m_Target.position, OnPathComplete);
        StartCoroutine(UpdatePath());
    }

    #region move in direction A*

    private IEnumerator PatrolBetweenPoints()
    {
        yield return new WaitForSeconds(4f); //wait before moving to the next point

        if (m_CurrentPatrolPoint == m_PatrolPoints.Length) //end of the list
            m_CurrentPatrolPoint = 0; //start over

        m_Seeker.StartPath(transform.position, m_PatrolPoints[m_CurrentPatrolPoint].position, OnPathComplete); //path to the next patrol point

        m_CurrentPatrolPoint++; //next patrol point
    }

    private IEnumerator UpdatePath()
    {
        if (m_Target != null)
        {
            //start a new path to the target
            m_Seeker.StartPath(transform.position, m_Target.position, OnPathComplete);

            yield return new WaitForSeconds(1f / UpdateRate);

            StartCoroutine(UpdatePath());
        }
    }

    private void OnPathComplete(Path path)
    {
        if (!path.error)
        {
            m_Path = path;
            m_CurrentWaypoint = 0;
        }
    }

    private void MoveInDirection()
    {
        if (m_Path != null & !m_IsDestroying)
        {
            if (m_CurrentWaypoint >= m_Path.vectorPath.Count)
            {
                if (m_PathIsEnded)
                    return;

                m_PathIsEnded = true;

                if (m_Target == null)
                {
                    m_RadarAnimator.SetBool("Threat", false);
                    StartCoroutine(PatrolBetweenPoints());
                }
            }
             else
            {
                m_PathIsEnded = false;

                var direction = (m_Path.vectorPath[m_CurrentWaypoint] - transform.position).normalized
                    * Speed * Time.fixedDeltaTime;

                m_Rigidbody.AddForce(direction, ForceMode2D.Force);

                if (Vector3.Distance(transform.position, m_Path.vectorPath[m_CurrentWaypoint]) < m_NextWaypointDistance)
                {
                    m_CurrentWaypoint++;
                }
            }
        }
    }

    private void SetOnDestroy(bool value)
    {
        m_IsDestroying = value;
        Destroy(m_RadarAnimator.gameObject);
    }

    #endregion
}
