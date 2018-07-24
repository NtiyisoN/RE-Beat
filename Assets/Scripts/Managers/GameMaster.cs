﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets._2D;

public class GameMaster : MonoBehaviour {

    #region Singleton
    public static GameMaster Instance;

    public void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        #region Initialize Managers
        Initialize("EventSystem");

        Initialize("AudioManager");

        Initialize("LoadSceneManager");

        Initialize("ScreenFaderManager");

        Initialize("PauseMenuManager");

        InitializeRespawnPoint();

        InitalizePlayerToRespawn();
        #endregion

        RespawnPlayer(false);
    }

    #endregion


    public Transform RespawnPoint;
    private GameObject m_PlayerToRespawn;

    public bool isPlayerDead;

    void Initialize(string name)
    {
        var gameObjectToInstantiate = Resources.Load(name);
        Instantiate(gameObjectToInstantiate);
    }

    void InitializeRespawnPoint()
    {
        if (RespawnPoint == null)
            RespawnPoint = GameObject.FindWithTag("RespawnPoint").transform;

        if (RespawnPoint == null)
        {
            Debug.LogError("GameMaster: Can't find Respawn Point on scene");
        }
    }

    void InitalizePlayerToRespawn()
    {
        m_PlayerToRespawn = Resources.Load("Player") as GameObject;

        if (m_PlayerToRespawn == null)
        {
            Debug.LogError("GameMaster: Can't fint Player to respawn");
        }
    }

    public void ChangeRespawnPoint(Transform respawnPointTransform)
    {
        if (respawnPointTransform != null)
        {
            if (!ReferenceEquals(RespawnPoint, respawnPointTransform))
            {
                if (RespawnPoint != null)
                    RespawnPoint.GetComponent<RespawnPoint>().SetActiveFlame(false);
                
                RespawnPoint = respawnPointTransform;
            }
        }
        else
        {
            Debug.LogError("GameMaster: can't reasign m_Respawn point, because new respawn point is null");
        }
    }

    public void RespawnPlayer(bool isPlayerDied)
    {
        if (RespawnPoint != null)
        {
            if (m_PlayerToRespawn != null)
            {
                if (isPlayerDied)
                    StartCoroutine(RespawnPlayerWithFade());
                else
                    RespawnPlayerWithoutFade();
            }
            else
            {
                Debug.LogError("GameMaster: Couldn't respawn player, because GameMaster couldn't load Player from Resources.");
            }
        }
        else
        {
            Debug.LogError("GameMaster: Can't respawn player, because GameMaster couldn't found RespawnPoint on scene.");
        }
    }

    private IEnumerator RespawnPlayerWithFade()
    {
        isPlayerDead = true;

        yield return new WaitForSeconds(1f);

        yield return ScreenFaderManager.Instance.FadeToBlack();

        Camera.main.GetComponent<Camera2DFollow>().ChangeCameraPosition(RespawnPoint.position);
        var playerTransform = RespawnPlayerWithoutFade();
        Camera.main.GetComponent<Camera2DFollow>().ChangeCameraTarget(playerTransform);

        isPlayerDead = false;

        yield return ScreenFaderManager.Instance.FadeToClear();
    }

    private Transform RespawnPlayerWithoutFade()
    {
        isPlayerDead = true;

        var playerGameObject = Instantiate(m_PlayerToRespawn, RespawnPoint.position, m_PlayerToRespawn.transform.rotation);

        isPlayerDead = false;

        return playerGameObject.transform;
    }
}
