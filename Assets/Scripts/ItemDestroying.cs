﻿using UnityEngine;

public class ItemDestroying : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            GetComponent<Animator>().SetTrigger("Destroy");
            Destroy(collision.gameObject);
        }
    }

}
