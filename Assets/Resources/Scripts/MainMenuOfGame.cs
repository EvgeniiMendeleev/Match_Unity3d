using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MainMenuOfGame : MonoBehaviour
{
    void Update()
    {
        if (Input.touchCount > 0 || Input.GetMouseButton(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(GetEndDot()), Vector3.zero);

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

    //Функция, которая возвращает данные от пользователя в зависимости от платформы.
    Vector3 GetEndDot()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        else
        {
            return Input.mousePosition;
        }
    }
}
