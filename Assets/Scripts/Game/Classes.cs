using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Reflection;
using UnityEngine.Events;
using System;
using UnityEngine.Windows;
using UnityEngine.UI;

public static class MyDataBase
{
    const string fileName = "VNWRPGE.db";
    static string DBPath = @".\data\";
    static SqliteConnection connection;
    static SqliteCommand command;
    static SqliteTransaction dbTrans;
    public static void AwakeDB()
    {
        OpenConn();
        command.CommandText = $"SELECT * FROM Saves";
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
                Saves.main.Add(new Save(reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4).Split(","), reader.GetInt32(5), reader.GetString(6).Split(",")));
        }
        CloseConn();

        OpenConn();
        command.CommandText = $"SELECT * FROM Options";
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            reader.Read();
            Options.resolutionName = reader.GetString(0);
            Options.resolutionWidth = reader.GetString(1);
            Options.resolutionHeight = reader.GetString(2);
            Options.volume = float.Parse(reader.GetString(3));
            Options.fullScreen = reader.GetBoolean(4);
        }
        CloseConn();

        OpenConn();
        command.CommandText = $"SELECT * FROM GameData";
        using (SqliteDataReader reader = command.ExecuteReader())
        {
            reader.Read();
            GameData.scene = reader.GetString(0);
            GameData.ramification = reader.GetInt32(1);
            GameData.stats = reader.GetString(2).Split(",").ToList();
            GameData.skills = reader.GetString(3).Split(",").ToList();
            GameData.points = reader.GetInt32(4);
            GameData.isFirstStart = reader.GetBoolean(5);
        }
        CloseConn();
    }
    private static void OpenConn()
    {
        connection = new SqliteConnection("Data Source=" + DBPath + fileName);
        command = new SqliteCommand(connection);
        connection.Open();
        dbTrans = connection.BeginTransaction();
    }
    private static void CloseConn()
    {
        dbTrans.Commit();
        connection.Close();
        command.Dispose();
    }
    public static void Query(string query)
    {
        OpenConn();
        command.CommandText = query;
        command.ExecuteNonQuery();
        CloseConn();
    }
    public static void DeleteSave(int index)
    {
        Query($"UPDATE Saves SET Name='', Scene='', Ramification=0, Stats='', Points=0, Skills=''  WHERE ID={index+1}");
        Saves.main[index].name = "";
        Saves.main[index].scene = "";
        Saves.main[index].ramification = 0;
        Saves.main[index].stats = new string[] { "" };
        Saves.main[index].points = 0;
        Saves.main[index].skills = new string[] { "" };
    }
    public static void UpdateSave(string name, int index)
    {
        Saves.main[index].name = name;
        Saves.main[index].scene = GameData.scene;
        Saves.main[index].ramification = GameData.ramification;
        Saves.main[index].stats = GameData.stats.ToArray();
        Saves.main[index].points = GameData.points;
        Saves.main[index].skills = GameData.skills.ToArray();
        string a = "";
        if (GameData.stats.Count != 1)
        {
            foreach (string item in GameData.stats)
                if (item != "")
                    a += item + ",";
        }
        string b = "";
        foreach (var item in GameData.skills)
        {
            b += item;
            if (item != GameData.skills.Last())
                b += ",";
        }
        Query($"UPDATE Saves SET Name='{name}', Scene='{GameData.scene}', Ramification={GameData.ramification}, Stats='{a}', Points={GameData.points}, Skills='{b}'  WHERE ID={index + 1}");
    }
    public static void UpdateGameData(int index=-1, bool isNew=false, bool clear=false)
    {
        if (isNew || clear)
        {
            GameData.scene = isNew ? "1" : "0";
            GameData.ramification = 0;
            GameData.stats = new List<string>() { "" };
            GameData.points = 0;
            GameData.skills = new List<string>() { "Обычный удар", "Усиление" };
            string b = "";
            foreach (var item in GameData.skills)
            {
                b += item;
                if (item != GameData.skills.Last())
                    b += ",";
            }
            Query($"UPDATE GameData SET Scene='{GameData.scene}', Ramification=0, Stats='', Points=0, Skills='{b}'");
        }
        else if(index!=-1)
        {
            GameData.scene = Saves.main[index].scene;
            GameData.ramification = Saves.main[index].ramification;
            GameData.stats = Saves.main[index].stats.ToList();
            GameData.points = Saves.main[index].points;
            GameData.skills = Saves.main[index].skills.ToList();
            string a = "";
            if (GameData.stats.Count != 1)
            {
                foreach (var item in GameData.stats)
                    if (item != "")
                        a += item + ",";
            }
            string b = "";
            foreach (var item in GameData.skills)
            {
                b += item;
                if (item != GameData.skills.Last())
                    b += ",";
            }
            Query($"UPDATE GameData SET Scene='{GameData.scene}', Ramification={GameData.ramification}, Stats='{a}', Points={GameData.points}, Skills='{b}'");
        }
        else
        {
            string a = "";
            if (GameData.stats.Count != 1)
            {
                foreach (var item in GameData.stats)
                    if (item != "")
                        a += item + ",";
            }
            string b = "";
            foreach (var item in GameData.skills)
            {
                b += item;
                if (item != GameData.skills.Last())
                    b += ",";
            }
            Query($"UPDATE GameData SET Scene='{GameData.scene}', Ramification={GameData.ramification}, Stats='{a}', Points={GameData.points}, Skills='{b}'");
        }
    }
}
public static class MyColors
{
    public static Color defColor = new Color(255, 255, 255);
    public static Color errorColor = new Color(255, 0, 0);
    public static ColorBlock SetColor(ColorBlock cb, string type)
    {
        if (type == "def")
            cb.normalColor = defColor;
        else if (type == "error")
            cb.normalColor = errorColor;
        return cb;

    }
}
public class Save
{
    public string name;
    public string scene;
    public int ramification;
    public string[] stats;
    public int points;
    public string[] skills;
    public Save(string name, string scene, int ramiflication, string[] stats, int points, string[] skills)
    {
        this.name = name;
        this.scene = scene;
        this.ramification = ramiflication;
        this.stats = stats;
        this.points = points;
        this.skills = skills;
    }
}
static class Saves
{
    public static List<Save> main = new();
}
public class SpecialSceneLink
{
    public string[] statNeed;
    public bool isOptional;
    public bool isFinalFrame;
    public string afterSceneText;
    public string link;
}
public class Variant
{
    public string text;
    public string[] statNeed;
    public string statNeedAction;
    public bool isOptional;
    public bool isFinalFrame;
    public string[] statGet;
    public SpecialSceneLink[] specialSceneLinks;
    public string[] defaultLink;
}
public class NPC
{
    public string name;
    public string side;
    public int[] turn;
    public string[] sprites;
    public int[] replicas;
}
public class Ramification
{
    public string sound;
    public string background;
    public string storyText;
    public Variant[] variants;
    public NPC[] npcs;
}
public class Scene
{
    public string scene;
    public List<Ramification> ramifications;
}
public class Battle
{
    public string scene;
    public string[] backgrounds;
    public List<Wave> waves;
}
public class Wave
{
    public List<string> mobs;
    public List<string> mobsPositions;
    public List<Mob> mobsStat = new();
    public List<GameObject> mobsObj = new();
    public List<string> mobsConditions = new();
    public float spawn;
    public void RemoveEnemy(int i)
    {
        this.mobs.RemoveAt(i);
        this.mobsObj.RemoveAt(i);
        this.mobsPositions.RemoveAt(i);
        this.mobsConditions.RemoveAt(i);
        this.mobsStat.RemoveAt(i);
    }
}
public class Mob
{
    public Mob(string name, float hp, float damage)
    {
        this.name = name;
        this.currentHp = hp;
        this.hp = hp;
        this.damage = damage;
    }
    public string name;
    public float hp;
    public float currentHp;
    public float damage;
}
public static class ObjectsData
{
    public static List<Scene> scenes;
    public static List<Scene> finals;
    public static List<Battle> battles;
    public static List<Mob> mobs;
    public static List<GameObject> mobsObj = new List<GameObject>();
    public static List<Skill> skills = new List<Skill>();
    public static void getPlot(JToken json, FieldInfo[] fields)
    {
        int i = 0;
        foreach (JProperty prop in json)
        {
            List<Scene> arrField = new();
            foreach (JToken token in prop.Value)
            {
                arrField.Add(token.ToObject<Scene>());
            }
            fields[i].SetValue(null, arrField);
            i++;
        }
    }
    public static void getBattleScenes(JArray btlJson, JArray mbJson, FieldInfo[] fields)
    {
        List<Battle> arrField = new();
        foreach (JToken token in btlJson)
        {
            arrField.Add(token.ToObject<Battle>());
        }
        fields[2].SetValue(null, arrField);
        List<Mob> arrField1 = new();
        foreach (JToken token in mbJson)
        {
            arrField1.Add(token.ToObject<Mob>());
        }
        fields[3].SetValue(null, arrField1);
    }
}
static class Options
{
    public static string resolutionName;
    public static string resolutionWidth;
    public static string resolutionHeight;
    public static float volume;
    public static bool fullScreen;
}
static class GameData
{
    public static string scene = "0";
    public static int ramification = 0;
    public static List<string> stats = new List<string> { "" };
    public static int points = 0;
    public static List<string> skills = new List<string> { "" };
    public static bool isFirstStart = false;
    public static int menuMusic = 0;
}
static class PlayerStats
{
    public static float hp = 100;
    public static float damageBuff = 1;
    public static int damageBuffTurns = 0;
    public static float damageDebuff = 1;
    public static int damageDebuffTurns = 0;
    public static int stan = 1;
    public static int[] skillscooldown = { 0, 0, 0, 0, 0 };
}
public class Skill
{
    public Skill(string name, string icon, string animation, string description, bool isPassive, bool isBuff, int cooldown, Func<Mob, bool> action)
    {
        this.name = name;
        this.icon = icon;
        this.animation = animation;
        this.description = description;
        this.isPassive = isPassive;
        this.isBuff = isBuff;
        this.cooldown = cooldown;
        this.action = action;
    }
    public string name;
    public string icon;
    public string animation;
    public string description;
    public bool isPassive;
    public bool isBuff;
    public int cooldown;
    public Func<Mob, bool> action;
}

public class Classes : MonoBehaviour
{
    public static void LoadEnemies(List<GameObject> enemies) { ObjectsData.mobsObj = enemies; }
    public static void LoadSkills() 
    {
        Func<Mob, bool> act = (enemy) => {
            Debug.Log("Attack");
            float damage = 40 * PlayerStats.damageBuff / PlayerStats.damageDebuff;
            enemy.currentHp -= damage;
            return enemy.currentHp > 0;
            };
        Skill skill = new("Обычный удар", "DefAttack", "Attack1",
                          "Нанесение 40 урона врагу",
                          false, false, 1, act);
        ObjectsData.skills.Add(skill);

        act = (enemy) => {
            Debug.Log("Buff");
            if (PlayerStats.damageBuff < 1.2f)
            {
                PlayerStats.damageBuff = 1.2f;
                PlayerStats.damageBuffTurns = 2;
            }
            return false;
        };
        skill = new("Усиление", "DamageBuff", "asd",
                          "Увеличение урона на 20% в течении 2 ходов",
                          false, true, 2, act);
        ObjectsData.skills.Add(skill);
    }
}
