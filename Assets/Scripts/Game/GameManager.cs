using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [SerializeField] private bool MainMenu = false, VN = false, RPG = false;
    const int PreLoadScene = 0, MainMenuScene = 1, VnPartScene = 2, RPGPartScene = 3;
    Dictionary<string, string> resolutions = new Dictionary<string, string>{ { "640x480", "4:3" },
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

    [SerializeField] VisualNovell visualNovell;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private  GameObject closingTab;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject newGameTab;
    [SerializeField] private GameObject continueGameTab;

    [SerializeField] private GameObject savesMenu;
    [SerializeField] private GameObject loadGameTab;
    [SerializeField] private GameObject deleteSaveTab;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject resolutionConfirmTab;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject Game;
    [SerializeField] private GameObject Default;
    [SerializeField] private GameObject saveNameTab;

    [SerializeField] private List<Button> Cells;
    [SerializeField] public AudioSource audio;

    int activeCell = 0;

    string[] resolutionBuff;
    string resolutionName = "";

    bool load = true;

    private void Start()
    {
        audio.volume = Options.volume;
        audio.timeSamples = GameData.menuMusic;
        GameData.menuMusic = 0;
        resolutionBuff = new string[3]{ Options.resolutionWidth, Options.resolutionHeight, "-1" };
        resolutionName = Options.resolutionName;
        Screen.SetResolution(Int32.Parse(Options.resolutionWidth), Int32.Parse(Options.resolutionHeight), Options.fullScreen);
        if (MainMenu)
        {
            Dropdown drop = optionsMenu.transform.GetChild(0).GetChild(1).GetComponent<Dropdown>();
            drop.ClearOptions();
            foreach (var item in resolutions)
                drop.options.Add(new Dropdown.OptionData($"{item.Key} {item.Value}"));

        }
    }
    #region 
    public void Play(string type)
    {
        if (type != "Back")
        {
            mainMenu.SetActive(false);
            playMenu.SetActive(true);

            if (GameData.scene == "0")
                GameObject.Find("ContinueGame").GetComponent<Button>().interactable = false;
            else
                GameObject.Find("ContinueGame").GetComponent<Button>().interactable = true;
        }
        else
        {
            mainMenu.SetActive(true);
            playMenu.SetActive(false);
        }
    }
    public void QiutBtn(string type)
    {
        if (closingTab.activeSelf == true && type != "Cancel")
            Application.Quit();
        else if (type == "Cancel")
        closingTab.SetActive(false);
        else
            closingTab.SetActive(true);
    }
    public void NewGame(string type)
    {
        if (newGameTab.activeSelf == true && type != "Cancel")
        {
            MyDataBase.UpdateGameData(isNew: true);
            SceneManager.LoadScene(VnPartScene);
        }
        else if (type == "Cancel")
            newGameTab.SetActive(false);
        else
            newGameTab.SetActive(true);
    }
    public void ContinueGame(string type)
    {
        if (continueGameTab.activeSelf == true && type != "Cancel")
            SceneManager.LoadScene(VnPartScene);
        else if (type == "Cancel")
            continueGameTab.SetActive(false);
        else
            continueGameTab.SetActive(true);
    }
    #endregion


    #region
    public void LoadSaves(string type)
    {
        if (type == "save")
        {
            load = false;
        }
        else if (type == "load")
        {
            load = true;
        }
        if (type != "Back")
        {
            if (MainMenu)
            playMenu.SetActive(false);
            else if (VN)
            {
                Default.SetActive(false);
                optionsMenu.SetActive(false);
            }
            savesMenu.SetActive(true);
            for (int i = 0; i < Saves.main.Count; i++)
            {
                if (Saves.main[i].name != "")
                    Cells[i].GetComponentInChildren<Text>().text = Saves.main[i].name;
                else
                Cells[i].GetComponentInChildren<Text>().text = "Пустое сохранение";
            }
        }
        else
        {
            playMenu.SetActive(true);
            savesMenu.SetActive(false);
        }
    }
    public void LoadBtn(string type)
    {
        if (load)
        {
            if (type != "Cancel" && loadGameTab.activeSelf == true)
            {
                MyDataBase.UpdateGameData(activeCell);
                SceneManager.LoadScene(VnPartScene);
            }
            else if (type == "Cancel")
            {
                loadGameTab.SetActive(false);
            }
            else if (Saves.main[Int32.Parse(type)].scene != "")
            {
                activeCell = Int32.Parse(type);
                loadGameTab.SetActive(true);
                loadGameTab.transform.GetChild(0).GetChild(0).gameObject.GetComponentInChildren<Text>().text = $"Вы уверены что хотите загрузить игру\n{Saves.main[Convert.ToInt32(type)].name}?";
            }
        }
    }
    public void OpenOptions(string type)
    {
        if (type != "Back" && type != "loadIndx" && type != "volumeChange")
        {
            mainMenu.SetActive(false);
            optionsMenu.SetActive(true);
            OpenOptions("loadIndx");
        }
        else if (type == "volumeChange")
        {
            audio.volume = optionsMenu.transform.GetChild(0).GetChild(2).GetComponent<Slider>().value;
        }
        else if (type == "loadIndx")
        {
            optionsMenu.transform.GetChild(0).GetChild(2).GetComponent<Slider>().value = Options.volume;
            Dropdown drop = optionsMenu.transform.GetChild(0).GetChild(1).GetComponent<Dropdown>();
            List<Dropdown.OptionData> el = drop.options;
            for (int i = 0; i < el.Count; i++)
            {
                if (el[i].text == Options.resolutionName)
                {
                    resolutionBuff[2] = i.ToString();
                    drop.value = i;
                    Debug.Log(el[i].text);
                    break;
                }
            }
            if (resolutionBuff[2] == "-1")
            {
                drop.options.Add(new Dropdown.OptionData(Options.resolutionName));
                resolutionBuff[2] = $"{el.Count() - 1}";
                drop.value = el.Count() - 1;
            }
        }
        else
        {
            mainMenu.SetActive(true);
            optionsMenu.SetActive(false);
            if (Options.volume != audio.volume)
            {
                Options.volume = audio.volume;
                MyDataBase.Query($"UPDATE Options SET Volume='{Options.volume}'");
            }
        }
    }
    public void Change(Dropdown drop)
    {
        if (drop.name == "ResolutionChange" && drop.value != Int32.Parse(resolutionBuff[2]))
        {
            List<Dropdown.OptionData> el = optionsMenu.transform.GetChild(0).GetChild(1).GetComponent<Dropdown>().options;
            string res = el[drop.value].text;
            Debug.Log(res+"asd");
            Options.resolutionName = res;
            Options.resolutionWidth = res.Split(" ")[0].Split("x")[0];
            Options.resolutionHeight = res.Split(" ")[0].Split("x")[1];
            Debug.Log($"Выбор: {res}");
            Screen.SetResolution(Int32.Parse(Options.resolutionWidth), Int32.Parse(Options.resolutionHeight), Options.fullScreen);
            Options.volume = audio.volume;
            MyDataBase.Query($"UPDATE Options SET Volume='{Options.volume}'");
            resolutionConfirmTab.SetActive(true);
        }
    }
    public void ConfirmChange(string type)
    {
        if (type == "Cancel")
        {
            resolutionConfirmTab.SetActive(false);
            Debug.Log($"Set: {resolutionName}");
            Options.resolutionName = resolutionName;
            Options.resolutionWidth = resolutionBuff[0];
            Options.resolutionHeight = resolutionBuff[1];
            Screen.SetResolution(Int32.Parse(Options.resolutionWidth), Int32.Parse(Options.resolutionHeight), Options.fullScreen);
            OpenOptions("loadIndx");
        }
        else
        {
            resolutionConfirmTab.SetActive(false);
            resolutionBuff = new string[] { Options.resolutionWidth, Options.resolutionHeight, "-1" };
            resolutionName = Options.resolutionName;
            Debug.Log($"Set: {resolutionName}");
            MyDataBase.Query($"UPDATE Options SET ResolutionName='{Options.resolutionName}', ResolutionWidth='{Options.resolutionWidth}', ResolutionHeight='{Options.resolutionHeight}'");
            OpenOptions("loadIndx");
        }
    }
    public void SavesBtnEnter(GameObject dltBtn)
    {
        if(dltBtn.transform.parent.GetChild(0).GetComponent<Text>().text != "Пустое сохранение")
            dltBtn.SetActive(true);
    }
    public void SavesBtnExit(GameObject dltBtn)
    {
        if (dltBtn.activeSelf)
            dltBtn.SetActive(false);
    }
    public void DeleteSave(string type)
    {
        if (deleteSaveTab.activeSelf == true && type != "Cancel")
        {
            MyDataBase.DeleteSave(activeCell);
            deleteSaveTab.SetActive(false);
            LoadSaves("");
        }
        else if (type == "Cancel")
        {
            deleteSaveTab.SetActive(false);
        }
        else
        {
            activeCell = Convert.ToInt32(type);
            deleteSaveTab.SetActive(true);
            deleteSaveTab.transform.GetChild(0).GetChild(0).gameObject.GetComponentInChildren<Text>().text = $"Вы хотите удалить сохранение\n{Saves.main[Convert.ToInt32(type)].name}?";
        }
    }
    #endregion


    #region
    public void ToMainMenu()
    {
        MyDataBase.UpdateGameData();
        SceneManager.LoadScene(MainMenuScene);
    }
    public void Pause()
    {
        visualNovell.pauseGame = !visualNovell.pauseGame;
        if (visualNovell.pauseGame)
        {
            pauseMenu.SetActive(true); 
        }
        else
        {
            visualNovell.afterPause = true;
            Default.SetActive(true);
            loadGameTab.SetActive(false);
            saveNameTab.SetActive(false);
            savesMenu.SetActive(false);
            optionsMenu.SetActive(false);
            pauseMenu.SetActive(false);
        }
    }
    public void SaveBtn(string type)
    {
        if (!load)
        {
            InputField inputF = saveNameTab.transform.GetChild(0).GetChild(3).gameObject.GetComponent<InputField>();
            if (saveNameTab.activeSelf == true && type != "Cancel" && inputF.text.Length > 0)
            {
                MyDataBase.UpdateSave(inputF.text, activeCell);
                saveNameTab.SetActive(false);
                inputF.text = "";
                inputF.colors = MyColors.SetColor(inputF.colors, "def");
                LoadSaves("");
            }
            else if (saveNameTab.activeSelf == true && type != "Cancel" && inputF.text.Length <= 0)
            {
                inputF.colors = MyColors.SetColor(inputF.colors, "error");
            }
            else if (type == "Cancel")
            {
                saveNameTab.SetActive(false);
            }
            else
            {
                saveNameTab.SetActive(true);
                activeCell = Convert.ToInt32(type);
            }
        }
    }
    #endregion
}
