using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Threading;

public class VisualNovell : MonoBehaviour
{
    const int PreLoadScene = 0, MainMenuScene = 1, VnPartScene = 2, RPGPartScene = 3;
    [SerializeField] private GameManager GM;

    public bool pauseGame;
    private Scene currentScene;

    private bool storyFrameChange;
    private float storyFrameChangeTimer;
    [SerializeField] private Text storyText;
    private string currentText;
    private bool textAnimSkip;
    public bool afterPause;
    private int textIndex;
    private bool lastStoryFrame;
    private readonly float[] variantRowPosY = new float[2] { -7f, -40f };
    private readonly float[] variantRowPosX = new float[5] { -242f, -124f, 0, 124f, 242f };

    [SerializeField] private Button variantBtn;
    [SerializeField] private GameObject gamePanel;
    private List<Button> variantBtns = new();
    [SerializeField] private Image background;
    [SerializeField] private Animator gameAnim;

    private bool isTransition;
    [SerializeField] private Text transitionText;

    private UnityAction nonBtnLink;
    private bool nonBtn;

    private NPC[] activeNPCs;
    [SerializeField] private Image[] NPCsides;
    [SerializeField] private Animator[] anims;

    private List<Variant> showVariants = new();
    private List<float> btnsPosX = new();
    private List<float> btnsPosY = new();
    private List<UnityAction> actions = new();

    string path = @"./Assets/Scripts/Logs/";

    private void Start()
    {
        GameData.scene = GameData.scene != null ? GameData.scene : "0";

        StartFrame();
    }
    private void StartFrame()
    {
        if (GameData.scene[0] == 'f')
            currentScene = ObjectsData.finals.Find(a => a.scene == GameData.scene);
        else
            currentScene = ObjectsData.scenes.Find(a => a.scene == GameData.scene);
        activeNPCs = currentScene.ramifications[GameData.ramification].npcs;

        anims[0].SetTrigger("clear");
        anims[0].ResetTrigger("appearance");
        anims[1].SetTrigger("clear");
        anims[1].ResetTrigger("appearance");
        anims[2].SetTrigger("clear");
        anims[2].ResetTrigger("appearance");

        textIndex = 0;
        lastStoryFrame = false;
        CheckBg();
        CheckSound();

        Thread sceneOpen = new(()=>OpenScene());
        sceneOpen.Start();

        storyText.text = "";
        currentText = currentScene.ramifications[GameData.ramification].storyText.Split("NF ")[textIndex];
        storyFrameChange = true; 
        
        CharactersAction(true);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) GM.Pause();
        if (!pauseGame && !isTransition)
        {
            if ((Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.Space))&&!afterPause)
            {
                if (!lastStoryFrame && !afterPause)
                {
                    if (storyFrameChange)
                    {
                        textAnimSkip = true;
                        storyFrameChangeTimer = 0f;
                    }
                    else
                    {
                        textIndex++;
                        storyText.text = "";
                        currentText = currentScene.ramifications[GameData.ramification].storyText.Split("NF ")[textIndex];
                        storyFrameChange = true;
                        CharactersAction(false);
                    }
                }
                else if (lastStoryFrame && !afterPause && nonBtn)
                    nonBtnLink();
                else if (lastStoryFrame && !afterPause && !nonBtn && GameData.scene[0] == 'f')
                {
                    MyDataBase.UpdateGameData(clear: true);
                    SceneManager.LoadScene(MainMenuScene);
                }
            }
            else if (afterPause) afterPause = false;
            if (storyFrameChange)
            {
                if (textAnimSkip)
                {
                    textAnimSkip = false;
                    storyFrameChange = false;
                    storyFrameChangeTimer = 0;
                    storyText.text = currentScene.ramifications[GameData.ramification].storyText.Split("NF ")[textIndex];
                    CheckLastStoryFrame();
                }
                else if (!textAnimSkip)
                    StoryFrame();
            }
        }
    }
    private void StoryFrame()
    {
        storyFrameChangeTimer += Time.deltaTime;
        if (storyFrameChangeTimer > 0.05)
        {
            storyText.text += currentText[0];
            currentText = currentText.Remove(0, 1);
            storyFrameChangeTimer = 0;
            if (currentText.Length <= 0)
            {
                storyFrameChangeTimer = 0;
                storyFrameChange = false;
                CheckLastStoryFrame();
            }
        }
    }
    private void CharactersAction(bool start = false)
    {
        foreach (NPC ch in activeNPCs)
        {
            bool turn = false;
            int num = 0;
            for (int i = 0; i < ch.turn.Length; i++)
                if (ch.turn[i] == textIndex + 1) 
                { 
                    turn = true; 
                    num = i;
                    break;
                }
            int side = ch.side == "left" ? 0 : ch.side == "mid" ? 1 : 2;
            if (turn)
            {
                NPCsides[side].sprite = Resources.Load<Sprite>($"characters/{ch.name}/{ch.sprites[num]}");
                if (start)
                {
                    anims[side].SetTrigger("clearTalk");
                    anims[side].ResetTrigger("startTalk");
                }
                if (NPCsides[side].color.a == 0 || start)
                {
                    anims[side].SetTrigger("appearance");
                    anims[side].ResetTrigger("clear");
                }
                var a = from x in ch.replicas where x == textIndex + 1 select x;
                if (a.Count() != 0)
                {
                    anims[side].SetTrigger("startTalk");
                    anims[side].ResetTrigger("clearTalk");
                }
                else
                {
                    anims[side].ResetTrigger("startTalk");
                    anims[side].SetTrigger("clearTalk");
                }
            }
            else
            {
                anims[side].ResetTrigger("appearance");
                anims[side].SetTrigger("clear");
                anims[side].ResetTrigger("startTalk");
                anims[side].SetTrigger("clearTalk");
            }
        }
    }
    private void CheckLastStoryFrame()
    {
        if (currentScene.ramifications[GameData.ramification].storyText.Split("NF ")[textIndex] == currentScene.ramifications[GameData.ramification].storyText.Split("NF ").Last())
        {
            lastStoryFrame = true;
            BtnsGenerate();
            variantBtns.ForEach(x => x.GetComponent<VariantScript>().Appearance());
        }
    }
    private void BtnsGenerate()
    {
        if (showVariants.Count() > 1)
        {
            for (int i = 0; i < showVariants.Count; i++)
            {
                Button a = Instantiate(variantBtn, gamePanel.transform, false);
                a.GetComponent<RectTransform>().localPosition = new Vector2(btnsPosX[i], btnsPosY[i]);
                a.transform.GetChild(0).GetComponent<Text>().text = showVariants[i].text;
                a.gameObject.SetActive(false);
                a.onClick.AddListener(actions[i]);
                variantBtns.Add(a);
            }
        }
        btnsPosX.Clear();
        btnsPosY.Clear();
        actions.Clear();
        showVariants.Clear();
    }
    private void OpenScene(bool ramificationChange = false)
    {
        if (currentScene != null)
        {
            //step 1
            if (GameData.scene[0] != 'f' && currentScene.ramifications[GameData.ramification].variants.Count() > 0)
            {
                foreach (Variant v in currentScene.ramifications[GameData.ramification].variants)
                {
                    if (!v.isOptional)
                    {
                        var a = from x in v.statNeed from y in GameData.stats where y == x select x;
                        if ((a.Count() > 0 && v.statNeedAction == "show") || (a.Count() == 0 && v.statNeedAction == "hide"))
                            showVariants.Add(v);
                    }
                    else
                    {
                        var a = from x in v.statNeed from y in GameData.stats where y == x select x;
                        if ((a.Count() == v.statNeed.Count() && v.statNeedAction == "show") || (a.Count() != v.statNeed.Count() && v.statNeedAction == "hide"))
                            showVariants.Add(v);
                    }
                }
                //step 2
                if (showVariants.Count() > 1)
                {
                    if (showVariants.Count() == 3 || showVariants.Count() == 5)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            btnsPosX.Add(variantRowPosX[i + i]);
                            btnsPosY.Add(showVariants.Count() == 3 ? variantRowPosY[1] - (variantRowPosY[1] + variantRowPosY[0]) / 2 : variantRowPosY[0]);
                        }
                        if (showVariants.Count() == 5)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                btnsPosX.Add(variantRowPosX[1 + i + i]);
                                btnsPosY.Add(variantRowPosY[1]);
                            }
                        }
                        else if (showVariants.Count() == 6)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                btnsPosX.Add(variantRowPosX[i + i]);
                                btnsPosY.Add(variantRowPosY[1]);
                            }
                        }
                    } // 3 & 5 & 6
                    else if (showVariants.Count() == 2 || showVariants.Count() == 4)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            btnsPosX.Add(variantRowPosX[1 + i + i]);
                            btnsPosY.Add(showVariants.Count() == 2 ? variantRowPosY[1] - (variantRowPosY[1] + variantRowPosY[0]) / 2 : variantRowPosY[0]);
                        }
                        if (showVariants.Count() == 4)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                btnsPosX.Add(variantRowPosX[1 + i + i]);
                                btnsPosY.Add(variantRowPosY[1]);
                            }
                        }
                    } // 2 & 4
                }

                //step 3
                for (int i = 0; i < showVariants.Count(); i++)
                {
                    UnityAction act = () => { };
                    List<string> statsToGet = new();
                    foreach (string item in showVariants[i].statGet)
                    {
                        if (GameData.stats.Find(x => x == item) == null)
                            statsToGet.Add(item);
                    }
                    if (statsToGet.Count() != 0)
                        act = () => GameData.stats.AddRange(statsToGet);
                    string sceneLink = "";
                    foreach (SpecialSceneLink link in showVariants[i].specialSceneLinks)
                    {
                        if (!link.isOptional)
                        {
                            var query = from x1 in link.statNeed from x2 in GameData.stats where x1 == x2 select x2;
                            if (query.Count() > 0)
                            {
                                sceneLink = link.link;
                                if (link.isFinalFrame)
                                {
                                    act += () => GameData.scene = sceneLink;
                                    act += () => GameData.ramification = 0;
                                    if (link.afterSceneText != "")
                                        act += () => SceneTransition(link.afterSceneText);
                                    else
                                    {
                                        act += () => StartFrame();
                                    }
                                }
                                else
                                {
                                    act += () => GameData.ramification = Int32.Parse(sceneLink);
                                    if (link.afterSceneText != "")
                                        act += () => SceneTransition(link.afterSceneText);
                                    else
                                    {
                                        act += () => StartFrame();
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            var query = from x1 in link.statNeed from x2 in GameData.stats where x1 == x2 select x2;
                            if (query.Count() == link.statNeed.Count())
                            {
                                sceneLink = link.link;
                                if (link.isFinalFrame)
                                {
                                    act += () => GameData.scene = sceneLink;
                                    act += () => GameData.ramification = 0;
                                    if (link.afterSceneText != "")
                                        act += () => SceneTransition(link.afterSceneText);
                                    else
                                    {
                                        act += () => StartFrame();
                                    }
                                }
                                else
                                {
                                    act += () => GameData.ramification = Int32.Parse(sceneLink);
                                    if (link.afterSceneText != "")
                                        act += () => SceneTransition(link.afterSceneText);
                                    else
                                    {
                                        act += () => StartFrame();
                                    }
                                }
                                break;
                            }
                        }
                    }
                    if (sceneLink == "")
                    {
                        string[] defLink = showVariants[i].defaultLink;
                        if (showVariants[i].isFinalFrame)
                        {
                            act += () => GameData.scene = defLink[0];
                            act += () => GameData.ramification = 0;
                            if (defLink[1] != "")
                                act += () => SceneTransition(defLink[1]);
                            else
                            {
                                act += () => StartFrame();
                            }
                        }
                        else
                        {
                            act += () => GameData.ramification = Int32.Parse(defLink[0]);
                            if (defLink[1] != "")
                                act += () => SceneTransition(defLink[1]);
                            else
                            {
                                act += () => StartFrame();
                            }
                        }
                    }
                    if (showVariants.Count() > 1)
                    {
                        act += () => variantBtns.ForEach(x => x.GetComponent<VariantScript>().Deactiv());
                        act += () => variantBtns.Clear();
                        actions.Add(act);
                    }
                    else
                    {
                        act += () => nonBtn = false;
                        nonBtnLink = act;
                        nonBtn = true;
                    }
                }
            }
        }
    }
    private void CheckSound()
    {
        Debug.Log(GM.audio.clip.name);
        if (GM.audio.clip.name != currentScene.ramifications[GameData.ramification].sound)
        {
            GM.audio.clip = Resources.Load<AudioClip>($"audio/novell/{currentScene.ramifications[GameData.ramification].sound}");
            GM.audio.Play();
        }
    }
    private void CheckBg()
    {
        if (!isTransition)
        {
            if (background.name != "Background" && background.name != currentScene.ramifications[GameData.ramification].background)
                SceneTransition("miniS");
            else if (background.name == "Background")
                SceneTransition("miniE");
        }
        else
        {
            Debug.Log($"{background.name} {currentScene.ramifications[GameData.ramification].background}");
            storyText.text = "";
            if (background.name == "Background" || (background.name != currentScene.ramifications[GameData.ramification].background))
            {
                Debug.Log("change");
                background.name = currentScene.ramifications[GameData.ramification].background;
                background.sprite = Resources.Load<Sprite>($"backgrounds/novell/{currentScene.ramifications[GameData.ramification].background}");
            }
        }
    }
    private void SceneTransition(string afterSceneText = "")
    {
        if (!isTransition)
        {
            isTransition = true;
            anims[0].SetTrigger("clear");
            anims[1].SetTrigger("clear");
            anims[2].SetTrigger("clear");
            if (afterSceneText == "miniS") gameAnim.SetTrigger("start");
            else if (afterSceneText == "miniE") gameAnim.SetTrigger("end");
            else
            {
                transitionText.text = afterSceneText;
                gameAnim.SetTrigger("transition");
            }
        }
        else
        {
            isTransition = false;
        }
    }
}
