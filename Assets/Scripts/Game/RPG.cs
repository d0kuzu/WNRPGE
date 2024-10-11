using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RPG : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private SpriteRenderer playerRender;
    private Animator anim;
    private float playerDir = 1;
    private Vector3 playerDefPos;
    [SerializeField] GameObject healthBar;
    public float playerHp;

    [SerializeField] GameObject bgParent;
    [SerializeField] GameObject enemiesParent;

    [SerializeField] private Battle currentScene;
    [SerializeField] GameObject background;

    private bool battle;
    private bool backToDefPos;

    private int enemyTurnInd;
    private Vector3 enemyStartPos;
    private bool enemyWalk;
    private int wave = -1;
    private GameObject selectedEnemy;
    private bool playerTurn = true;
    private Skill activeSkill;
    private Mob playerTarget;
    private bool attack;


    List<Vector3> vanguard = new();
    List<Vector3> rearguard = new();

    [SerializeField] GameObject[] skillBtns;
    void Start()
    {
        playerHp = PlayerStats.hp;
        playerRender = player.GetComponent<SpriteRenderer>();
        anim = player.GetComponent<Animator>();

        OpenScene();
        RefreshSkillBar();
    }
    void Update()
    {
        if(!battle)
            CameraFollow();
        if (!battle) { PlayerMove(); CheckEnemy(); }
        else if (enemyWalk) GoToPositions();
        else if (playerTurn && !attack)
        {
            ChooseEnemy();
        }
        else if (attack)
        {
            if (playerTurn)
                PlayerMove(true);
            else
                EnemyMove(enemyTurnInd);
        }
    }
    private void NextTurn()
    {
        for (int i = 0; i < PlayerStats.skillscooldown.Length; i++)
            if (PlayerStats.skillscooldown[i] != 0)
                PlayerStats.skillscooldown[i] -= 1;
        RefreshSkillBar();
    }
    private void CameraFollow()
    {
        Vector3 dir = new Vector3(player.transform.position.x + (2.5f * playerDir), Camera.main.transform.position.y, Camera.main.transform.position.z);
        Camera.main.transform.position = Vector3.Slerp(Camera.main.transform.position, dir, 4 * Time.deltaTime);
    }
    private void PlayerMove(bool animMove= false)
    {
        Vector3 dir;
        if (!animMove)
        {
            float x = Input.GetAxisRaw("Horizontal");
            playerDir = Input.GetAxisRaw("Horizontal") < 0 ? -1 : Input.GetAxisRaw("Horizontal") > 0 ? 1 : playerDir;
            anim.SetBool("Run", x != 0);
            dir = new Vector3(player.transform.position.x + (2.5f * x), player.transform.position.y, player.transform.position.z);
        }
        else
        {
            anim.SetBool("Run", true);
            if(backToDefPos)
                dir = new Vector3(playerDefPos.x, playerDefPos.y, playerDefPos.z);
            else 
                dir = new Vector3(selectedEnemy.transform.position.x - 1, selectedEnemy.transform.position.y, player.transform.position.z);

            playerDir = player.transform.position.x < dir.x ? 1 : player.transform.position.x > dir.x ? -1 : playerDir;
        }
        playerRender.flipX = playerDir < 0 ? true : playerDir > 0 ? false : playerRender.flipX;
        player.transform.position = Vector3.MoveTowards(player.transform.position, dir, 6 * Time.deltaTime);
        if (player.transform.position == dir && animMove)
        {
            anim.SetBool("Run", false);
            if (!backToDefPos)
                PlayerAttack("start");
            else
            {
                backToDefPos = false;
                playerTurn = false;
                playerDir = 1;
                playerRender.flipX = false;
                EnemyAttack("start");
            }
        }

    }
    private void EnemyMove(int index)
    {
        Vector3 dir;
        Vector3 current = currentScene.waves[wave].mobsObj[index].transform.position;
        Animator enemyAnim = currentScene.waves[wave].mobsObj[index].GetComponent<Animator>();
        enemyAnim.SetBool("Run", true);
        if (backToDefPos)
            dir = new Vector3(enemyStartPos.x, enemyStartPos.y, enemyStartPos.z);
        else
            dir = new Vector3(player.transform.position.x + 1, player.transform.position.y, current.z);

        float enemyDir = current.x < dir.x ? 1 : current.x > dir.x ? -1 : 0;
        SpriteRenderer enemyRender = currentScene.waves[wave].mobsObj[index].GetComponent<SpriteRenderer>();
        enemyRender.flipX = enemyDir < 0 ? false : enemyDir > 0 ? true : enemyRender.flipX;
        currentScene.waves[wave].mobsObj[index].transform.position = Vector3.MoveTowards(current, dir, 6 * Time.deltaTime);
        if (current == dir)
        {
            enemyAnim.SetBool("Run", false);
            enemyAnim.SetTrigger("Idle");
            if (!backToDefPos) 
            {
                enemyAnim.SetTrigger("Attack");
                anim.SetTrigger("Hurt"); 
                playerHp -= currentScene.waves[wave].mobsStat[index].damage;
                backToDefPos = true;
                RefreshHealthBars();
            }
            else
            {
                attack = false;
                backToDefPos = false;
                playerTurn = true;
                enemyRender.flipX = false;
                NextTurn();
            }
        }
    }
    private void OpenScene()
    {
        currentScene = ObjectsData.battles.Find(a => a.scene == GameData.scene);
        if(currentScene != null)
        {
            // step 1
            float x = -10;
            foreach (string item in currentScene.backgrounds)
            {
                GameObject bg = Instantiate(background, new Vector3(x, 0, bgParent.transform.position.z), Quaternion.Euler(0, 0, 0), bgParent.transform);
                SpriteRenderer render = bg.GetComponent<SpriteRenderer>();
                render.sprite = Resources.Load<Sprite>($"backgrounds/rpg/{item}");
                render.size = new Vector2(1, 1);

                x += 17.8f;
            }
            // step 2
            foreach (Wave wave in currentScene.waves)
            {
                foreach (string mobName in wave.mobs)
                {
                    Mob mob = ObjectsData.mobs.Find(a => a.name == mobName);
                    wave.mobsStat.Add(new Mob(mob.name, mob.hp, mob.damage));
                    wave.mobsObj.Add(Instantiate(ObjectsData.mobsObj.Find(a => a.name == mobName), 
                                                    new Vector3(UnityEngine.Random.Range(wave.spawn - 4,wave.spawn + 4), UnityEngine.Random.Range(-1.5f, -4), player.transform.position.z), 
                                                    Quaternion.Euler(0,0,0), 
                                                    enemiesParent.transform));
                    wave.mobsConditions.Add("Idle");
                }
            }
        }
    }
    private void CheckEnemy()
    {
        for (int i = 0; i < currentScene.waves.Count; i++)
        {
            if(player.transform.position.x - currentScene.waves[i].spawn > -10)
            {
                wave = i;
                battle = true;
                enemyWalk = true;
                playerDir = 1;
                anim.SetBool("Run", false);
                // step 1
                foreach (string item in currentScene.waves[i].mobsPositions)
                {
                    if (item[0] == '1') vanguard.Add(new Vector3());
                    else if (item[0] == '2') rearguard.Add(new Vector3());
                }
                // step 2.1
                float z = enemiesParent.transform.position.z;
                if (vanguard.Count == 3 || vanguard.Count == 1)
                {
                    vanguard[0] = new Vector3(player.transform.position.x + 5, player.transform.position.y, z);
                    if(vanguard.Count == 3)
                    {
                        vanguard[1] = new Vector3(player.transform.position.x + 5, player.transform.position.y, z);
                        z += 0.1f;
                        float y = 1;
                        for (int j = 0; j < 3; j+=2)
                        {
                            vanguard[j] = new Vector3(player.transform.position.x + 6, player.transform.position.y + y, z);
                            z -= 0.2f;
                            y *= -1;
                        }
                    }
                }
                else if(vanguard.Count == 2 || vanguard.Count == 4)
                {
                    float y = 1;
                    for (int j = 0; j < 2; j++)
                    {
                        vanguard[j] = new Vector3(player.transform.position.x + 5, player.transform.position.y + y, z);
                        z -= 0.1f;
                        y *= -1;
                    }
                    z = enemiesParent.transform.position.z;
                    if (vanguard.Count == 4)
                    {
                        y = 1;
                        for (int j = 1; j < 3; j++)
                        {
                            vanguard[j] = new Vector3(player.transform.position.x + 5, player.transform.position.y + y, z);
                            z -= 0.1f;
                            y *= -1;
                        }
                        z += 0.3f;
                        y = 2;
                        for (int j = 0; j < 4; j+=3)
                        {
                            vanguard[j] = new Vector3(player.transform.position.x + 6, player.transform.position.y + y, z);
                            z -= 0.3f;
                            y *= -1;
                        }
                    }
                }
                // step 2.2
                if (rearguard.Count == 3 || rearguard.Count == 1)
                {
                    rearguard[0] = new Vector3(player.transform.position.x + 7, player.transform.position.y, z);
                    if (rearguard.Count == 3)
                    {
                        rearguard[1] = new Vector3(player.transform.position.x + 7, player.transform.position.y, z);
                        z += 0.1f;
                        float y = 1;
                        for (int j = 0; j < 3; j += 2)
                        {
                            rearguard[j] = new Vector3(player.transform.position.x + 8, player.transform.position.y + y, z);
                            z -= 0.2f;
                            y *= -1;
                        }
                    }
                }
                else if (rearguard.Count == 2 || rearguard.Count == 4)
                {
                    float y = 1;
                    for (int j = 0; j < 2; j++)
                    {
                        rearguard[j] = new Vector3(player.transform.position.x + 7, player.transform.position.y + y, z);
                        z -= 0.1f;
                        y *= -1;
                    }
                    z = enemiesParent.transform.position.z;
                    if (rearguard.Count == 4)
                    {
                        y = 1;
                        for (int j = 1; j < 3; j++)
                        {
                            rearguard[j] = new Vector3(player.transform.position.x + 7, player.transform.position.y + y, z);
                            z -= 0.1f;
                            y *= -1;
                        }
                        z += 0.3f;
                        y = 2;
                        for (int j = 0; j < 4; j += 3)
                        {
                            rearguard[j] = new Vector3(player.transform.position.x + 8, player.transform.position.y + y, z);
                            z -= 0.3f;
                            y *= -1;
                        }
                    }
                }
            }
        }
    }
    private void GoToPositions()
    {
        int van = 0;
        int rear = 0;
        int comp = 0;
        for (int i = 0; i < currentScene.waves[wave].mobs.Count; i++)
        {
            if (currentScene.waves[wave].mobsPositions[i][0] == '1')
            {
                if (currentScene.waves[wave].mobsObj[i].transform.position == vanguard[van])
                { 
                    comp++;
                    if (currentScene.waves[wave].mobsConditions[i]=="Run") { currentScene.waves[wave].mobsObj[i].GetComponent<Animator>().SetTrigger("Idle"); currentScene.waves[wave].mobsConditions[i] = "Idle"; }
                }
                else if(currentScene.waves[wave].mobsConditions[i] != "Death")
                {
                    if (currentScene.waves[wave].mobsConditions[i] =="Idle") { currentScene.waves[wave].mobsObj[i].GetComponent<Animator>().SetTrigger("Run"); currentScene.waves[wave].mobsConditions[i] = "Run"; }
                    currentScene.waves[wave].mobsObj[i].transform.position = Vector3.MoveTowards(currentScene.waves[wave].mobsObj[i].transform.position, vanguard[van], 6 * Time.deltaTime);
                }
                van++;
            }
            else if (currentScene.waves[wave].mobsPositions[i][0] == '2')
            {
                if (currentScene.waves[wave].mobsObj[i].transform.position == rearguard[rear])
                {
                    comp++;
                    if (currentScene.waves[wave].mobsConditions[i]=="Run") { currentScene.waves[wave].mobsObj[i].GetComponent<Animator>().SetTrigger("Idle"); currentScene.waves[wave].mobsConditions[i] = "Idle"; }
                }
                else if (currentScene.waves[wave].mobsConditions[i] != "Death")
                {
                    if (currentScene.waves[wave].mobsConditions[i]=="Idle") { currentScene.waves[wave].mobsObj[i].GetComponent<Animator>().SetTrigger("Run"); currentScene.waves[wave].mobsConditions[i] = "Run"; }
                    currentScene.waves[wave].mobsObj[i].transform.position = Vector3.MoveTowards(currentScene.waves[wave].mobsObj[i].transform.position, rearguard[rear], 6 * Time.deltaTime);
                }
                rear++;
            }
        }
        if (comp == vanguard.Count + rearguard.Count) { enemyWalk = false; }
    }
    private void ChooseEnemy()
    {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.up);
        if(hit.collider != null && hit.collider.tag == "Enemy")
        {
            if (Input.GetButtonDown("Fire1"))
            {
                int van;
                CheckForVanguard(out van);
                int index = currentScene.waves[wave].mobsObj.IndexOf(hit.collider.gameObject);
                if ((currentScene.waves[wave].mobsPositions[index][0] == '1' || van == 0) && currentScene.waves[wave].mobsConditions[index] != "Death")
                {
                    if (selectedEnemy != null)
                        selectedEnemy.transform.GetChild(0).gameObject.SetActive(false);
                    selectedEnemy = hit.transform.gameObject;
                    selectedEnemy.transform.GetChild(0).gameObject.SetActive(true);
                }
            }
        }
    }
    private void RefreshSkillBar()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < GameData.skills.Count && GameData.skills[i] != "")
            {
                Skill skill = ObjectsData.skills.Find(a => a.name == GameData.skills[i]);
                skillBtns[i].GetComponent<Image>().sprite = Resources.Load<Sprite>($"rpg/skills/{skill.icon}");
                Transform cdText = skillBtns[i].transform.GetChild(0);
                if (PlayerStats.skillscooldown[i] != 0)
                {
                    cdText.gameObject.SetActive(true);
                    cdText.GetComponent<Text>().text = $"{PlayerStats.skillscooldown[i]}";
                }
                else
                {
                    cdText.gameObject.SetActive(false);
                }
            }
            else
                skillBtns[i].GetComponent<Image>().sprite = Resources.Load<Sprite>($"rpg/skills/Empty");
        }
    }
    private void RefreshHealthBars()
    {
        // player Hp
        if(playerHp <= 0)
        {
            anim.SetTrigger("Death");
            playerHp = 0;
        }
        Vector3 healthBg = healthBar.transform.GetChild(0).localScale;
        healthBar.transform.GetChild(1).localScale = new Vector3(healthBg.x / PlayerStats.hp * playerHp, healthBg.y);
        healthBar.transform.GetChild(2).GetComponent<Text>().text = $"{playerHp}/{PlayerStats.hp}";
        // enemies Hp
        for (int i = 0; i < currentScene.waves[wave].mobsObj.Count; i++)
        {
            if (currentScene.waves[wave].mobsConditions[i] != "Death")
            {
                if (currentScene.waves[wave].mobsStat[i].currentHp <= 0)
                {
                    currentScene.waves[wave].mobsStat[i].currentHp = 0;
                    currentScene.waves[wave].mobsObj[i].GetComponent<Animator>().SetTrigger("Death");
                    currentScene.waves[wave].mobsConditions[i] = "Death";
                }
                healthBg = currentScene.waves[wave].mobsObj[i].transform.GetChild(1).localScale;
                currentScene.waves[wave].mobsObj[i].transform.GetChild(2).localScale = new Vector3(healthBg.x / currentScene.waves[wave].mobsStat[i].hp * currentScene.waves[wave].mobsStat[i].currentHp, healthBg.y);
            }
        }
    }
    public void SkillUse(int num)
    {
        if(GameData.skills.Count-1 > num && !attack)
        {
            int index = currentScene.waves[wave].mobsObj.IndexOf(selectedEnemy);
            Skill skill = ObjectsData.skills.Find(a => a.name == GameData.skills[num]);
            if ((skill.isBuff || selectedEnemy != null) && PlayerStats.skillscooldown[num] == 0)
            {
                if (selectedEnemy != null)
                    selectedEnemy.transform.GetChild(0).gameObject.SetActive(false);
                PlayerStats.skillscooldown[num] = skill.cooldown;
                if (!skill.isBuff)
                {
                    activeSkill = skill;
                    playerTarget = currentScene.waves[wave].mobsStat[index];
                    playerDefPos = player.transform.position;
                    attack = true;
                    RefreshSkillBar();
                }
                else
                {
                    anim.SetTrigger(skill.animation);
                    skill.action.Invoke(currentScene.waves[wave].mobsStat[index]);
                    selectedEnemy = null;
                    playerTurn = false;
                    RefreshHealthBars();
                    EnemyAttack("start");
                }
            }
        }
    }
    private void CheckForVanguard(out int van)
    {
        van = 0;
        for (int i = 0; i < currentScene.waves[wave].mobs.Count; i++)
            if (currentScene.waves[wave].mobsConditions[i] != "Death" && currentScene.waves[wave].mobsPositions[i][0] == '1')
                van++;
    }
    private void EnemyAttack(string type)
    {
        if(type == "start")
        {
            int van;
            CheckForVanguard(out van);

            List<int> aliveEnemies = new();
            for (int i = 0; i < currentScene.waves[wave].mobs.Count; i++)
            {
                if (currentScene.waves[wave].mobsConditions[i] != "Death")
                {
                    if ((currentScene.waves[wave].mobsPositions[i][0] == '1' && van > 0) || (currentScene.waves[wave].mobsPositions[i][0] != '1' && van == 0))
                        aliveEnemies.Add(i);
                }
            }
            //Debug.Log();
            enemyTurnInd = UnityEngine.Random.Range(0, aliveEnemies.Count);
            enemyStartPos = currentScene.waves[wave].mobsObj[enemyTurnInd].transform.position;
        }
    }
    private void Deied()
    {
        Debug.Log("gg");
    }
    private void PlayerAttack(string type)
    {
        if(type == "start")
        {
            anim.SetTrigger(activeSkill.animation);
            activeSkill.action.Invoke(playerTarget);
            selectedEnemy.GetComponent<Animator>().SetTrigger("Hurt");
            attack = false;
            RefreshHealthBars();
        }
        else
        {
            attack = true;
            backToDefPos = true;
            selectedEnemy = null;
        }
    }
}
