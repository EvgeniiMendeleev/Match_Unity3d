using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MainMenuOfGame : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount > 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position), Vector3.zero);
            
            if (hit)
            {
                if (hit.collider.name == "Play_Main_Menu")
                {
                    hit.collider.GetComponent<AudioSource>().Play();
                    SceneManager.LoadScene(1);
                }
            }
        }
    }
}
