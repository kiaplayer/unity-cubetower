﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class GameController : MonoBehaviour
{
    private CubePos nowCube = new CubePos(0, 1, 0);
    public float cubeChangePlaceSpeed = 0.5f;
    public Transform cubeToPlace;
    private float camMoveToYPosition, camMoveSpeed = 2f;
    public Text scoreTxt;
    public GameObject[] cubesToCreate;
    public GameObject allCubes, vfx;
    public GameObject[] canvasStartPage;
    private Rigidbody allCubesRb;
    private bool isLose, firstCube;
    private Coroutine showCubePlace;

    private readonly List<Vector3> allCubesPositions = new List<Vector3> {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(-1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(0, 0, -1),
        new Vector3(1, 0, 1),
        new Vector3(-1, 0, -1),
        new Vector3(-1, 0, 1),
        new Vector3(1, 0, -1),
    };

    public Color[] bgColors;
    private Color toCameraColor;
    private int prevCountMaxHorizontal;
    private Transform mainCam;

    private List<GameObject> possibleCubesToCreate = new List<GameObject>();

    private void Start()
    {
        AddPossibleCubes(PlayerPrefs.GetInt("score") / 5);
        scoreTxt.text = "<size=40><color=#E05156>best:</color></size> " + PlayerPrefs.GetInt("score") + "\n<size=22>now:</size> 0";
        toCameraColor = Camera.main.backgroundColor;
        mainCam = Camera.main.transform;
        camMoveToYPosition = 5.9f + nowCube.y - 1f;
        allCubesRb = allCubes.GetComponent<Rigidbody>();
        showCubePlace = StartCoroutine(ShowCubePlace());
    }

    private void Update()
    {
        if ((Input.GetMouseButtonDown(0) || Input.touchCount > 0) && cubeToPlace != null && allCubes != null && !EventSystem.current.IsPointerOverGameObject())
        {
#if !UNITY_EDITOR
			if (Input.GETTOUCH(0).phase != TouchPhase.Began) return;
#endif

            if (!firstCube)
            {
                firstCube = true;
                foreach (GameObject obj in canvasStartPage)
                {
                    Destroy(obj);
                }
            }

            GameObject createCube = null;
            if (possibleCubesToCreate.Count == 1)
                createCube = possibleCubesToCreate[0];
            else
                createCube = cubesToCreate[UnityEngine.Random.Range(0, possibleCubesToCreate.Count)];
            
            GameObject newCube = Instantiate(createCube, cubeToPlace.position, Quaternion.identity);
            newCube.transform.SetParent(allCubes.transform);
            nowCube.SetVector(cubeToPlace.position);
            allCubesPositions.Add(nowCube.GetVector());

            GameObject newVfx = Instantiate(vfx, newCube.transform.position, Quaternion.identity);
            Destroy(newVfx, 1.5f);

            if (PlayerPrefs.GetString("music") != "No")
            {
                GetComponent<AudioSource>().Play();
            }

            allCubesRb.isKinematic = true;
            allCubesRb.isKinematic = false;
            SpawnPosition();
            MoveCameraChangeBg();
        }


        if (!isLose && allCubesRb.velocity.magnitude > 0.1f)
        {
            Destroy(cubeToPlace.gameObject);
            StopCoroutine(showCubePlace);
            isLose = true;
        }

        mainCam.localPosition = Vector3.MoveTowards(
        	mainCam.localPosition,
            new Vector3(mainCam.localPosition.x, camMoveToYPosition, mainCam.localPosition.z),
            camMoveSpeed * Time.deltaTime
        );

        if (Camera.main.backgroundColor != toCameraColor)
        {
        	Camera.main.backgroundColor = Color.Lerp(Camera.main.backgroundColor, toCameraColor, Time.deltaTime / 1.5f);
        }
    }

    IEnumerator ShowCubePlace()
    {
        while (true)
        {
            SpawnPosition();
            yield return new WaitForSeconds(cubeChangePlaceSpeed);
        }
    }

    private void SpawnPosition()
    {
        List<Vector3> positions = new List<Vector3>();
        if (IsPositionEmpty(new Vector3(nowCube.x + 1, nowCube.y, nowCube.z)) && nowCube.x + 1 != cubeToPlace.position.x)
            positions.Add(new Vector3(nowCube.x + 1, nowCube.y, nowCube.z));
        if (IsPositionEmpty(new Vector3(nowCube.x - 1, nowCube.y, nowCube.z)) && nowCube.x - 1 != cubeToPlace.position.x)
            positions.Add(new Vector3(nowCube.x - 1, nowCube.y, nowCube.z));
        if (IsPositionEmpty(new Vector3(nowCube.x, nowCube.y + 1, nowCube.z)) && nowCube.y + 1 != cubeToPlace.position.y)
            positions.Add(new Vector3(nowCube.x, nowCube.y + 1, nowCube.z));
        if (IsPositionEmpty(new Vector3(nowCube.x, nowCube.y - 1, nowCube.z)) && nowCube.y - 1 != cubeToPlace.position.y)
            positions.Add(new Vector3(nowCube.x, nowCube.y - 1, nowCube.z));
        if (IsPositionEmpty(new Vector3(nowCube.x, nowCube.y, nowCube.z + 1)) && nowCube.z + 1 != cubeToPlace.position.z)
            positions.Add(new Vector3(nowCube.x, nowCube.y, nowCube.z + 1));
        if (IsPositionEmpty(new Vector3(nowCube.x, nowCube.y, nowCube.z - 1)) && nowCube.z - 1 != cubeToPlace.position.z)
            positions.Add(new Vector3(nowCube.x, nowCube.y, nowCube.z - 1));
        cubeToPlace.position = positions[UnityEngine.Random.Range(0, positions.Count)];
    }

    private bool IsPositionEmpty(Vector3 targetPos)
    {
        if (targetPos.y == 0)
            return false;
        foreach (Vector3 pos in allCubesPositions)
            if (pos.x == targetPos.x && pos.y == targetPos.y && pos.z == targetPos.z)
                return false;
        return true;
    }

    private void MoveCameraChangeBg()
    {
        int maxX = 0, maxY = 0, maxZ = 0, maxHor;

        foreach (Vector3 pos in allCubesPositions)
        {
            if (Mathf.Abs(Convert.ToInt32(pos.x)) > maxX)
                maxX = Convert.ToInt32(pos.x);
            if (Convert.ToInt32(pos.y) > maxY)
                maxY = Convert.ToInt32(pos.y);
            if (Mathf.Abs(Convert.ToInt32(pos.z)) > maxZ)
                maxZ = Convert.ToInt32(pos.z);
        }


        if (PlayerPrefs.GetInt("score") < maxY - 1)
        {
            PlayerPrefs.SetInt("score", maxY - 1);
        }
        
        scoreTxt.text = "<size=40><color=#E05156>best:</color></size> " + PlayerPrefs.GetInt("score") + "\n<size=22>now:</size> " + (maxY - 1);
        
        camMoveToYPosition = 5.9f + nowCube.y - 1f;

        maxHor = maxX > maxZ ? maxX : maxZ;
        if (maxHor % 3 == 0 && prevCountMaxHorizontal != maxHor)
        {
            mainCam.localPosition -= new Vector3(0, 0, 2.5f);
            prevCountMaxHorizontal = maxHor;
        }

        if (maxY >= 20)
            toCameraColor = bgColors[3];
        else if (maxY >= 15)
            toCameraColor = bgColors[2];
        else if (maxY >= 10)
            toCameraColor = bgColors[1];
        else if (maxY >= 5)
            toCameraColor = bgColors[0];
    }
    
    private void AddPossibleCubes(int till)
    {
        for (int i = 0; i <= till && i < cubesToCreate.Length; i++)
        {
            possibleCubesToCreate.Add(cubesToCreate[i]);
        }
    }
}

struct CubePos
{
    public int x, y, z;

    public CubePos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3 GetVector()
    {
        return new Vector3(x, y, z);
    }

    public void SetVector(Vector3 pos)
    {
        x = Convert.ToInt32(pos.x);
        y = Convert.ToInt32(pos.y);
        z = Convert.ToInt32(pos.z);
    }
}