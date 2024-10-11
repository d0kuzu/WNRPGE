using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadMenu : MonoBehaviour
{
    const int PreLoadScene = 0, MainMenuScene = 1, VnPartScene = 2, RPGPartScene = 3;
    Dictionary<string, string> resolutions = new Dictionary<string, string>{ { "640x360", "16:9" },
                                                                             { "800x600", "4:3" },
                                                                             { "1024x720", "4:3" },
                                                                             { "1280x720", "16:9" },
                                                                             { "1280x800", "16:10" },
                                                                             { "1280x1024", "5:4" },
                                                                             { "1360x768", "16:9" },
                                                                             { "1366x768", "16:9" },
                                                                             { "1440x900", "16:10" },
                                                                             { "1536x864", "16:9" },
                                                                             { "1600x900", "16:9" },
                                                                             { "1680x1050", "16:10" },
                                                                             { "1920x1080", "16:9" }};
    [SerializeField] AudioSource audio;
    [SerializeField] Slider slider;
    [SerializeField] GameObject volumePanel;

    [SerializeField] List<GameObject> enemies;
    private void Start()
    {
        MyDataBase.AwakeDB();
        //ObjectsData.getBattleScenes(JsonConvert.DeserializeObject<JArray>(Resources.Load<TextAsset>("battles").ToString()), JsonConvert.DeserializeObject<JArray>(Resources.Load<TextAsset>("mobs").ToString()), typeof(ObjectsData).GetFields(BindingFlags.Public | BindingFlags.Static));
        ObjectsData.getPlot(JsonConvert.DeserializeObject<JToken>(Resources.Load<TextAsset>("scenes").ToString()), typeof(ObjectsData).GetFields(BindingFlags.Public | BindingFlags.Static));
        //ObjectsData.getBattleScenes(JsonConvert.DeserializeObject<JArray>(Resources.Load<TextAsset>("battles").ToString()), JsonConvert.DeserializeObject<JArray>(Resources.Load<TextAsset>("mobs").ToString()), typeof(ObjectsData).GetFields(BindingFlags.Public | BindingFlags.Static));
        if (Options.resolutionName == "")
        {
            string res = $"{Display.main.systemWidth}x{Display.main.systemHeight}";
            string val = "";
            resolutions.TryGetValue(res, out val);
            if (val != null)
            {
                Options.resolutionName = res+" "+val;
                Options.resolutionWidth = res.Split("x")[0];
                Options.resolutionHeight = res.Split("x")[1];
            }
            else
            {
                Options.resolutionName = res;
                Options.resolutionWidth = res.Split("x")[0];
                Options.resolutionHeight = res.Split("x")[1];
            }
        }
        MyDataBase.Query($"UPDATE Options SET ResolutionName='{Options.resolutionName}', ResolutionWidth='{Options.resolutionWidth}', ResolutionHeight='{Options.resolutionHeight}'");
        Screen.SetResolution(Int32.Parse(Options.resolutionWidth), Int32.Parse(Options.resolutionHeight), true);
        //Classes.LoadEnemies(enemies);
        //Classes.LoadSkills();
        if (GameData.isFirstStart) volumePanel.SetActive(true);
        else MainMenu();
    }
    private void Update()
    {
        audio.volume = slider.value;
    }
    public void MainMenu(string type="")
    {
        if (type == "set")
        {
            Options.volume = audio.volume;
            MyDataBase.Query($"UPDATE Options SET Volume='{Options.volume}'");
            GameData.isFirstStart = false;
            MyDataBase.Query($"UPDATE GameData SET IsFirstStart=0");
            GameData.menuMusic = audio.timeSamples;
        }
        SceneManager.LoadScene(MainMenuScene);
    }
}
