using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

enum SearchOfMatch { horizontal, vertical, angle }      //Метод проверки совпадений.

/*
 * Класс Point отвечает за координату фишки
 * на игровом поле для их перемещения и нахождение допустимой клетки
 * для хода.
 */
public sealed class Point
{
    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public int GetX { get { return x; } }
    public int GetY { get { return y; } }

    public int SetX { set { x = value; } }
    public int SetY { set { y = value; } }

    private int x;
    private int y;
}

public sealed class Game : MonoBehaviour
{
    private GameObject Selector;                            //Префаб селектора, чтобы пользователь видел, какую фишку он выбрал.
    private float z0 = -0.01f;                              //Нулевая координата z для фишек, чтобы они не сливались с фоном.
    private Point p0, target;                               //p0 - точка фишки, которую выбрали первой, target - точка фишки, которую выбрали для обмена с первой.
    
    private GameObject firstCake, secondCake;               //Префабы, необходимые для взаимодействия с фишками, которые выбрали.
    private bool isMovable = false;                         //Переменная, которая отвечает за действия перемещения объектов.
    private bool wasTurn = false;                           //Переменная, отвечающая за информацию о том: был ли сделан ход или нет.

    private const float distanceBetweenCells = 0.438f;          //Растояние между клетками, необходимое для расстановки фишек на поле.
    private const uint MaxHorizontal = 8, MaxVertical = 8;      //Размерность поля по вертикали и горизонтали.
    private GameObject[,] fieldObjects;                         //Поле с фишками.
    private Vector3 positionOfFirstCell;                        //Позиция первой ячейки для расстановки фишек.
    private bool isDown = true;                                 //Переменная, которая переключает на проверку: Все ли фишки после совпадения находятся на своих местах или нет.
    private bool isEnd = false;                                 //Переменная, отвечающая вывод меню об окончании игры.

    [SerializeField] private TextMesh scoreText;                //Текст с очками.
    private ushort score = 0;                                   //Само количество очков.

    [SerializeField] private AudioClip matchSound;              //Звук совпадения.
    [SerializeField] private AudioClip endSound;                //Звук, который проигрывается при достижении 4000 очков.
    [SerializeField] private AudioClip choiceButton;            //Звук при нажатии кнопки.
    [SerializeField] private AudioSource backgroundMusic;       //Музыка, которая играет на заднем фоне.
    [SerializeField] private GameObject matchEffect;            //Эффект, повяляющийся при совпадении фишек.
    [SerializeField] private GameObject pauseMenu;              //Меню паузы.
    [SerializeField] private GameObject endGameMenu;            //Меню конца игры.
    private bool isPause = false;                               //Переменная, отвечающая за перевод игры в состояние паузы.

    //--------Переменные, необходимые для создания таймера--------
    private float startTime;               //Переменная, которая содержит время начало игры.
    private float dt = 5.0f;               //Время, через которое начинается сама игра.
    //------------------------------------------------------------

    public List<GameObject> food;           //Пул фишек для их генерации на поле.

    //Генерируем фишки на поле в методе Start()
    void Start()
    {
        positionOfFirstCell = transform.GetChild(0).transform.position;

        fieldObjects = new GameObject[8,8];

        for (int i = 0; i < MaxHorizontal; i++)
        {
            for (int j = 0; j < MaxVertical; j++)
            {
                fieldObjects[i, j] = Instantiate(food[Random.Range(0, food.Count)], new Vector3(positionOfFirstCell.x + j * distanceBetweenCells, positionOfFirstCell.y + distanceBetweenCells, z0), Quaternion.identity);
                fieldObjects[i, j].GetComponent<Cake>().SetTarget = new Point(j, i);
            }
        }

        startTime = Time.time + dt;
    }

    //Основное взаимодействие пользователя с игрой в FixedUpdate().
    void FixedUpdate()
    {
        if (isPause)
        {
            GetComponent<AudioSource>().clip = choiceButton;

            Vector3 endPosition = new Vector3(0, 0.65f, -0.3f);
            pauseMenu.transform.position = Vector3.Lerp(pauseMenu.transform.position, endPosition, 0.11f);

            if (pauseMenu.transform.position == endPosition)
            {
                if (Input.touchCount > 0 || Input.GetMouseButton(0))
                {
                    RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(GetEndDot()), Vector3.zero);
                    
                    if (hit)
                    {
                        if (hit.collider.name == "Menu_Button")
                        {
                            GetComponent<AudioSource>().Play();
                            SceneManager.LoadScene(0);
                        }
                        else if (hit.collider.name == "Reload_Button")
                        {
                            GetComponent<AudioSource>().Play();
                            SceneManager.LoadScene(1);
                        }
                        else if (hit.collider.name == "Play_Button")
                        {
                            GetComponent<AudioSource>().Play();
                            backgroundMusic.Play();
                            isPause = false;
                        }
                    }
                }
            }
        }
        else if (!isEnd)
        {
            if (isMovable)      //Если выполнилось условие, что фишки можно двигать, то меняем их местами.
            {
                //Находим координаты второй фишки на сцене для фишки, которую мы выбрали первой, чтобы переместить её на позицию второй фишки.
                float posX = target.GetX * distanceBetweenCells + transform.GetChild(0).transform.position.x;
                float posY = -target.GetY * distanceBetweenCells + transform.GetChild(0).transform.position.y;

                Vector3 secondPosition = new Vector3(posX, posY, z0);

                //Находим координаты первой фишки на сцене для второй фишки.
                posX = p0.GetX * distanceBetweenCells + transform.GetChild(0).transform.position.x;
                posY = -p0.GetY * distanceBetweenCells + transform.GetChild(0).transform.position.y;

                Vector3 firstPosition = new Vector3(posX, posY, z0);

                //Меняем их местами, пока фишки не встанут на свои места.
                if (firstCake.transform.position == secondPosition && secondCake.transform.position == firstPosition)
                {
                    isMovable = false;
                }
            }
            else if (!isDown)
            {
                int count = 0;

                for (int i = 0; i < MaxVertical; i++)
                {
                    for (int j = 0; j < MaxHorizontal; j++)
                    {
                        float posX = fieldObjects[i, j].GetComponent<Cake>().GetX * distanceBetweenCells + transform.GetChild(0).transform.position.x;
                        float posY = -fieldObjects[i, j].GetComponent<Cake>().GetY * distanceBetweenCells + transform.GetChild(0).transform.position.y;

                        Vector3 newPos = new Vector3(posX, posY, z0);

                        if (fieldObjects[i, j] && fieldObjects[i, j].transform.position == newPos)
                        {
                            ++count;
                        }
                    }
                }

                if (count == (MaxHorizontal * MaxVertical))
                {
                    isDown = true;
                }
            }
            else
            {
                Vector3 endPosition = new Vector3(-5.15f, 0.95f, -3f);
                pauseMenu.transform.position = Vector3.Lerp(pauseMenu.transform.position, endPosition, 0.11f);

                if (pauseMenu.transform.position == endPosition)
                {
                    if (Time.time > startTime)
                    {
                        /*
                         * Изначально проверяем совпадения. Если совпадения не были найдены и был ход, то переставляем выбранные ячейки
                         * для обмена обратно на свои места. Проверка поля будет происходит в любом случае, даже если не было хода,
                         * так как в начале логического выражения стоит функция checkAllMatch().
                         */

                        if (score >= 4000)
                        {
                            isEnd = true;
                            backgroundMusic.clip = endSound;
                            backgroundMusic.loop = false;
                            backgroundMusic.Play();

                            startTime = Time.time + 1.5f;

                            return;
                        }

                        if (!checkAllMatch())
                        {
                            if (wasTurn)
                            {
                                isMovable = true;
                                wasTurn = false;

                                var temp = p0;
                                p0 = target;
                                target = temp;

                                Swap(ref p0, ref target);

                                firstCake.GetComponent<Cake>().SetTarget = target;
                                secondCake.GetComponent<Cake>().SetTarget = p0;

                                return;
                            }
                        }
                        else
                        {
                            scoreText.text = System.Convert.ToString(score);
                            wasTurn = false;

                            return;
                        }

                        //Читаем данные от пользователя.
                        if (Input.touchCount > 0 || Input.GetMouseButton(0))
                        {
                            //Проверяем, какой объект на поле пользователь задел.
                            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(GetEndDot()), Vector3.zero);

                            if (hit)
                            {
                                if (hit.collider.tag == "Cake")
                                {
                                    //Получаем информацию о расположении фишки в массиве, через координаты объекта на сцене.
                                    int i = System.Convert.ToInt32(Mathf.Abs(hit.collider.transform.position.y - transform.GetChild(0).transform.position.y) / distanceBetweenCells);
                                    int j = System.Convert.ToInt32(Mathf.Abs(hit.collider.transform.position.x - transform.GetChild(0).transform.position.x) / distanceBetweenCells);

                                    //Если ни одна фишка не выбрана, то выделяем ту, которую выбрали.
                                    if (!Selector)
                                    {
                                        Selector = Instantiate(Resources.Load<GameObject>("Prefabs/selector"), hit.collider.transform.position, Quaternion.identity);

                                        p0 = new Point(j, i);
                                        firstCake = fieldObjects[i, j];
                                    }
                                    else
                                    {
                                        target = new Point(j, i);
                                        secondCake = fieldObjects[i, j];

                                        //Если фишка была выбрана, то проверяем на допустимость их перемещения и уничтожаем селектор.
                                        if (AssetTouch(ref p0, ref target))
                                        {
                                            Swap(ref p0, ref target);

                                            firstCake.GetComponent<Cake>().SetTarget = target;
                                            secondCake.GetComponent<Cake>().SetTarget = p0;

                                            isMovable = true;
                                            wasTurn = true;
                                        }

                                        if (p0.GetX != target.GetX || p0.GetY != target.GetY) Destroy(Selector);
                                    }
                                }
                                else if (hit.collider.name == "Pause_Button")
                                {
                                    isPause = true;
                                    backgroundMusic.Stop();
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (startTime < Time.time)
            {
                GetComponent<AudioSource>().clip = choiceButton;
                Vector3 endPosition = new Vector3(0, 0.65f, -0.3f);
                endGameMenu.transform.position = Vector3.Lerp(endGameMenu.transform.position, endPosition, 0.11f);

                if (endGameMenu.transform.position == endPosition)
                {
                    if (Input.touchCount > 0 || Input.GetMouseButton(0))
                    {
                        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(GetEndDot()), Vector3.zero);
                        if (hit)
                        {
                            if (hit.collider.name == "Menu_Button")
                            {
                                GetComponent<AudioSource>().Play();
                                SceneManager.LoadScene(0);
                            }
                            else if (hit.collider.name == "Reload_Button")
                            {
                                GetComponent<AudioSource>().Play();
                                SceneManager.LoadScene(1);
                            }
                        }
                    }
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

    IEnumerator DownPieces()
    {
        isDown = false;

        yield return new WaitForFixedUpdate();

        for (int i = 0; i < MaxVertical; i++)
        {
            for (int j = 0; j < MaxHorizontal; j++)
            {
                if (fieldObjects[i, j] == null)
                {
                    for (int i1 = i - 1; i1 >= 0; i1--)
                    {
                        if(fieldObjects[i1, j]) fieldObjects[i1, j].GetComponent<Cake>().SetTarget = new Point(j, i1 + 1);

                        var tempObj = fieldObjects[i1 + 1, j];
                        fieldObjects[i1 + 1, j] = fieldObjects[i1, j];
                        fieldObjects[i1, j] = tempObj;
                    }
                }
            }
        }

        for (int i = 0; i < MaxHorizontal; i++)
        {
            for (int j = 0; j < MaxVertical; j++)
            {
                if (fieldObjects[i, j] == null)
                {
                    fieldObjects[i, j] = Instantiate(food[Random.Range(0, food.Count)], new Vector3(positionOfFirstCell.x + j * distanceBetweenCells, positionOfFirstCell.y + distanceBetweenCells, z0), Quaternion.identity);
                    fieldObjects[i, j].GetComponent<Cake>().SetTarget = new Point(j, i);
                }
            }
        }
    }

    //Простая функция обмена значений местами.
    private void Swap(ref Point d0, ref Point d1)
    {
        GameObject temp = fieldObjects[d0.GetY, d0.GetX];
        fieldObjects[d0.GetY, d0.GetX] = fieldObjects[d1.GetY, d1.GetX];
        fieldObjects[d1.GetY, d1.GetX] = temp;
    }

    private bool DeleteCakes(ref List<GameObject> list)
    {
        if (list.Count > 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                GameObject temp = Instantiate(matchEffect, new Vector3(list[i].transform.position.x, list[i].transform.position.y, -0.01f), matchEffect.transform.rotation);
                Destroy(list[i]);
                Destroy(temp, 1.0f);
            }

            GetComponent<AudioSource>().PlayOneShot(matchSound);

            list.Clear();
            StartCoroutine(DownPieces());

            return true;
        }
        else 
        {
            return false;
        }
    }

    //Функция проверки совпадений.
    private bool checkAllMatch()
    {
        List<GameObject> deletingCakes = new List<GameObject>();

        bool resAngle = checkMatch(SearchOfMatch.angle, ref deletingCakes);                //Проверяем пять фишек углом
        if (DeleteCakes(ref deletingCakes)) return true;

        bool resHorizontal = checkMatch(SearchOfMatch.horizontal, ref deletingCakes);      //Проверяем фишки горизонтально.
        if (DeleteCakes(ref deletingCakes)) return true;

        bool resVertical = checkMatch(SearchOfMatch.vertical, ref deletingCakes);          //Проверяем фишки вертикально.
        if (DeleteCakes(ref deletingCakes)) return true;

        return false;
    }

    private bool checkMatch(SearchOfMatch method, ref List<GameObject> deletingCakes)
    {
        bool isMatch = false;       //Было ли хотя бы одно совпадение

        switch (method)
        {
            //Проверка фишек по горизонтали.
            case SearchOfMatch.horizontal:

                for (int i = 0; i < MaxVertical; i++)
                {
                    int matchCount = 1;

                    for (int j = 0; j < (MaxHorizontal - 1); j++)
                    {
                        if (fieldObjects[i, j].name != fieldObjects[i, j + 1].name)
                        {
                                isMatch = true;
                                if (matchCount > 2)
                                {
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 10;
                                }
                                if (matchCount > 3) 
                                {
                                    deletingCakes.Add(fieldObjects[i, j - 3]);
                                    score += 10;
                                }
                                if (matchCount > 4)
                                { 
                                    deletingCakes.Add(fieldObjects[i, j - 4]);
                                    score += 15;
                                }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    //Если в конце строки у нас есть совпадение из фишек.
                    if (matchCount > 2)
                    {
                        isMatch = true;
                        if (matchCount > 2)
                        {
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                            score += 10;
                        }
                        if (matchCount > 3) 
                        {
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 4]);
                            score += 10;
                        }
                        if (matchCount > 4) 
                        {
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 5]);
                            score += 15;
                        }
                    }
                }

                break;

            //Проверка фишек вертикально.
            case SearchOfMatch.vertical:

                for (int j = 0; j < MaxHorizontal; j++)
                {
                    int matchCount = 1;

                    for (int i = 0; i < (MaxVertical - 1); i++)
                    {
                        if (fieldObjects[i, j].name != fieldObjects[i + 1, j].name)
                        {
                            if (matchCount > 2)
                            {
                                isMatch = true;
                                if (matchCount > 2)
                                {
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i - 1, j]);
                                    deletingCakes.Add(fieldObjects[i - 2, j]);

                                    score += 10;
                                }
                                if (matchCount > 3)
                                {
                                    deletingCakes.Add(fieldObjects[i - 3, j]);
                                    score += 10;
                                }
                                if (matchCount > 4)
                                {
                                    deletingCakes.Add(fieldObjects[i - 4, j]);
                                    score += 15;
                                }
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    //Если в конце столбца у нас есть совпадение из фишек.
                    if (matchCount > 2)
                    {
                        isMatch = true;

                        if (matchCount > 2)
                        {
                            deletingCakes.Add(fieldObjects[MaxVertical - 1, j]);
                            deletingCakes.Add(fieldObjects[MaxVertical - 2, j]);
                            deletingCakes.Add(fieldObjects[MaxVertical - 3, j]);

                            score += 10;
                        }
                        if (matchCount > 3) 
                        {
                            deletingCakes.Add(fieldObjects[MaxVertical - 4, j]);
                            score += 10;
                        }
                        if (matchCount > 4)
                        {
                            deletingCakes.Add(fieldObjects[MaxVertical - 5, j]);
                            score += 15;
                        }
                    }
                }

                break;

            //Проверка фишек на совпадение углом (только для пяти фишек!)
            case SearchOfMatch.angle:

                for (int i = 0; i < (MaxVertical - 2); i++)
                {
                    int matchCount = 1;

                    for (int j = 0; j < (MaxHorizontal - 1); j++)
                    {
                        /* Данное условие нужно, если у нас может быть совпадение следующего вида:
                         * .................
                         * ..........5......
                         * ..........5......
                         * .....555555......
                         * .................
                         */
                        if ((j <= (MaxHorizontal - 2)) && (matchCount == 3) && (fieldObjects[i, j + 1].name == fieldObjects[i, j].name))
                        {
                            bool isAngle = false;

                            if (fieldObjects[i + 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i + 2, j - 2].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;
                                
                                deletingCakes.Add(fieldObjects[i + 1, j - 2]);
                                deletingCakes.Add(fieldObjects[i + 2, j - 2]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

                                score += 35;
                            }
                            else if (fieldObjects[i + 1, j].name == fieldObjects[i, j].name && fieldObjects[i + 2, j].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;

                                deletingCakes.Add(fieldObjects[i + 1, j]);
                                deletingCakes.Add(fieldObjects[i + 2, j]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

                                score += 35;
                            }
                            else if (i > 1)
                            {
                                if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    isAngle = true;
                                    matchCount = 1;

                                    deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                    deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 35;
                                }
                                else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    isAngle = true;
                                    matchCount = 1;

                                    deletingCakes.Add(fieldObjects[i - 1, j]);
                                    deletingCakes.Add(fieldObjects[i - 2, j]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 35;
                                }
                            }

                            if (!isAngle) --matchCount;
                        }

                        if (fieldObjects[i, j].name != fieldObjects[i, j + 1].name)
                        {
                            if (matchCount == 3)
                            {
                                if (fieldObjects[i + 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i + 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;

                                    deletingCakes.Add(fieldObjects[i + 1, j - 2]);
                                    deletingCakes.Add(fieldObjects[i + 2, j - 2]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 35;
                                }
                                else if (fieldObjects[i + 1, j].name == fieldObjects[i, j].name && fieldObjects[i + 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;

                                    deletingCakes.Add(fieldObjects[i + 1, j]);
                                    deletingCakes.Add(fieldObjects[i + 2, j]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 35;
                                }
                                else if (i > 1)
                                {
                                    if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                    {
                                        isMatch = true;

                                        deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                        deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                        deletingCakes.Add(fieldObjects[i, j]);
                                        deletingCakes.Add(fieldObjects[i, j - 1]);
                                        deletingCakes.Add(fieldObjects[i, j - 2]);

                                        score += 35;
                                    }
                                    else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                    {
                                        isMatch = true;

                                        deletingCakes.Add(fieldObjects[i - 1, j]);
                                        deletingCakes.Add(fieldObjects[i - 2, j]);
                                        deletingCakes.Add(fieldObjects[i, j]);
                                        deletingCakes.Add(fieldObjects[i, j - 1]);
                                        deletingCakes.Add(fieldObjects[i, j - 2]);

                                        score += 35;
                                    }
                                }
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    /* Если у нас в конце строки есть совпадение углом, то есть:
                     * ..............
                     * .............4
                     * .............4
                     * ...........444
                     * ..............
                     */
                    if (matchCount == 3)
                    {
                        if (fieldObjects[i + 1, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i + 2, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name)
                        {
                            isMatch = true;

                            deletingCakes.Add(fieldObjects[i + 1, MaxHorizontal - 3]);
                            deletingCakes.Add(fieldObjects[i + 2, MaxHorizontal - 3]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                            score += 35;
                        }
                        else if (fieldObjects[i + 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i + 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                        {
                            isMatch = true;

                            deletingCakes.Add(fieldObjects[i + 1, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i + 2, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                            score += 35;
                        }
                        if (i > 1)
                        {
                            if (fieldObjects[i - 1, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name)
                            {
                                isMatch = true;

                                deletingCakes.Add(fieldObjects[i - 1, MaxHorizontal - 3]);
                                deletingCakes.Add(fieldObjects[i - 2, MaxHorizontal - 3]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                                score += 35;
                            }
                            else if (fieldObjects[i - 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                            {
                                isMatch = true;

                                deletingCakes.Add(fieldObjects[i - 1, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i - 2, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                                score += 35;
                            }
                        }
                    }
                }

                /* Проверка оставшихся двух строк в поле на совпадения вида:
                 *  ...................
                 *  5..................
                 *  5..................
                 *  555................
                 */ 
                for (int i = System.Convert.ToInt32(MaxVertical - 2); i < MaxVertical; i++)
                {
                    int matchCount = 1;

                    for (int j = 0; j < (MaxHorizontal - 1); j++)
                    {
                        if ((j <= (MaxHorizontal - 2)) && (matchCount == 3) && (fieldObjects[i, j + 1].name == fieldObjects[i, j].name))
                        {
                            bool isAngle = false;

                            if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;

                                deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

                                score += 35;
                            }
                            else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;

                                deletingCakes.Add(fieldObjects[i - 1, j]);
                                deletingCakes.Add(fieldObjects[i - 2, j]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

                                score += 35;
                            }

                            if (!isAngle) --matchCount;
                        }
                        if (fieldObjects[i, j].name != fieldObjects[i, j + 1].name)
                        {
                            if (matchCount == 3)
                            {
                                if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;

                                    deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                    deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 35;
                                }
                                else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;

                                    deletingCakes.Add(fieldObjects[i - 1, j]);
                                    deletingCakes.Add(fieldObjects[i - 2, j]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    score += 35;
                                }
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    if (fieldObjects[i - 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                    {
                        isMatch = true;
                        matchCount = 1;

                        deletingCakes.Add(fieldObjects[i - 1, MaxHorizontal - 1]);
                        deletingCakes.Add(fieldObjects[i - 2, MaxHorizontal - 1]);
                        deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                        deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                        deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                        score += 35;
                    }
                }

                break;
        }

        return isMatch;
    }

    //Функция проверки на допустимость перемещения клеток со своими координатами p0 и target.
    private bool AssetTouch(ref Point p0, ref Point target)
    {
        if(Mathf.Abs(target.GetY - p0.GetY) == 0)           //Если фишки находятся на одной строке.
        {
            if(Mathf.Abs(target.GetX - p0.GetX) == 1)       //Если фишки находятся в разных столбцах, где они являются соседними.
            {
                return true;
            }
        }
        else if(Mathf.Abs(target.GetX - p0.GetX) == 0)      //Если фишки находятся в одном столбце.
        {
            if(Mathf.Abs(target.GetY - p0.GetY) == 1)       //Если фишки находятся на разных строк, где они являются соседними.
            {
                return true;
            }
        }

        return false;
    }
}