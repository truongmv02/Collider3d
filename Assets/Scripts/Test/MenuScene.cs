using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuScene : MonoBehaviour
{
    public Button custom;
    public Button unity;

    public InputField input;

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 500;
        custom.onClick.AddListener(() => SceneManager.LoadScene("CustomCollider"));
        unity.onClick.AddListener(() => SceneManager.LoadScene("ColliderUnity"));
        input.onValueChanged.AddListener((a) => { PlayerPrefs.SetInt("count", int.Parse(a)); });
    }
}