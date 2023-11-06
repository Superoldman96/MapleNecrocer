﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.MapRender2;
using Microsoft.Xna.Framework;
using WzComparerR2.Rendering;
using MonoGame.SpriteEngine;
using GameUI;
using MonoGame.UI.Forms;

namespace MapleNecrocer;

public enum GameMode { Play, Viewer };

public struct MapNameRec
{
    public string ID;
    public string MapName;
    public string StreetName;

    public MapNameRec(string id, string mapName, string streetName)
    {
        ID = id;
        MapName = mapName;
        StreetName = streetName;
    }
}

public class Map
{
    public class FadeScreen
    {
        public static int AlphaCounter, AValue;
        public static bool DoFade;
    }
    public static Dictionary<string, MapNameRec> MapNameList = new();
    public static string ID;
    public static Wz_Node Img;
    public static Microsoft.Xna.Framework.Point DisplaySize = new(1024, 768);
    public static Dictionary<string, int> Info = new();
    public static int Left, Top, Right, Bottom, SaveMapBottom;
    public static bool ShowTile = true;
    public static bool ShowObj = true;
    public static bool ShowBack = true;
    public static bool ShowFront = true;
    public static bool ShowNpc = true;
    public static bool ShowNpcName = true;
    public static bool ShowNpcChat = true;
    public static bool ShowMob = true;
    public static bool ShowMobName = false;
    public static bool ShowID = false;
    public static bool ShowPortal = true;
    public static bool ShowBgmName;
    public static bool ShowFootholds;
    public static bool ShowPlayer = true;
    public static bool ShowMiniMap=true;

    public static Vector2 Center;
    public static Vector2 CameraSpeed;
    public static int OffsetY;
    public static ResourceLoader ResLoader;
    public static bool FirstLoaded;
    public static bool ReLoad;
    public static bool UseD2D = true;
    public static string NpcNameTagFont;
    public static string NpcBalloonFont;
    public static string MobLvFont;
    public static string ToolTipFont;
    public static GameMode GameMode = GameMode.Play;
    public static bool ResetPos;
    public static bool SaveMap;
    public static string BgmName;
    private static List<string> BgmList = new();
    static MiniMap MiniMap;

    public static int MeasureStringX(string FontNameKey, string Text)
    {
        if (UseD2D)
            return (int)EngineFunc.D2DFonts[FontNameKey].MeasureString(Text).X;
        else
            return (int)EngineFunc.Fonts[FontNameKey].MeasureString(Text).X;
    }

    public static void CreateResLoader()
    {
        if (ResLoader != null)
        {
            ResLoader.ClearAnimationCache();
            ResLoader.Dispose();
        }
        ResLoader = new ResourceLoader(RenderFormDraw.Instance.GraphicsDevice);
    }
    public static void LoadMap(string ID)
    {
        if (ID == null)
            return;
        if (EngineFunc.SpriteEngine.SpriteList != null)
        {
            foreach (var I in EngineFunc.SpriteEngine.SpriteList)
            {
                if (I.Tag != 1)
                    I.Dead();


                if (I is Mob)
                {
                    var Mob = I as Mob;
                    if (Mob.RenderTarget != null)
                    {
                        Mob.RenderTarget.Dispose();
                    }
                }
            }

            EngineFunc.SpriteEngine.Dead();
        }
        Wz.Data.Clear();
        //  if(Wz.Data!=null)
        foreach (var i in Wz.ImageLib)
            i.Value.Dispose();
        Wz.ImageLib.Clear();


        if (EngineFunc.SpriteEngine.ImageLib != null)
        {
            foreach (var Iter in EngineFunc.SpriteEngine.ImageLib)
                Iter.Value.Dispose();
        }

        if (EngineFunc.SpriteEngine.ImageLib != null)
            EngineFunc.SpriteEngine.ImageLib.Clear();

        //
        string LeftNum = ID.LeftStr(1);
        Map.Img = Wz.GetNode("Map/Map/Map" + LeftNum + "/" + ID + ".img");


        Map.Info.Clear();
        foreach (var Iter in Map.Img.GetNode("info").Nodes)
            Map.Info.Add(Iter.Text, Iter.ToInt());

        Map.Info.Add("MapWidth", Map.Img.GetValue2("miniMap/width", 0));
        Map.Info.Add("MapHeight", Map.Img.GetValue2("miniMap/height", 0));
        Map.Info.Add("centerX", Map.Img.GetValue2("miniMap/centerX", DisplaySize.X / 2));
        Map.Info.Add("centerY", Map.Img.GetValue2("miniMap/centerY", DisplaySize.Y / 2));


        MapPortal.Create();
        FootholdTree.CreateFootholds();
        if (Map.Info.ContainsKey("VRLeft"))
        {
            EngineFunc.SpriteEngine.Camera.X = Map.Info["VRLeft"];
            EngineFunc.SpriteEngine.Camera.Y = Map.Info["VRBottom"]; // - DisplaySize.y;
            Map.Left = Map.Info["VRLeft"];
            Map.Bottom = Map.Info["VRBottom"] + 15;
            if (Map.Img.GetNode("miniMap") != null)
            {
                int Bottom2 = -Map.Info["centerY"] + Map.Info["MapHeight"] - 55;
                if (Map.Bottom < Bottom2 - 100)
                    Map.Bottom = Bottom2;
            }
            Map.Top = Map.Info["VRTop"];
            Map.Right = Map.Info["VRRight"];
            Map.Info.AddOrReplace("MapWidth", Map.Right - Map.Left);
            //Map.Info.AddOrReplace("MapHeight", Math.Abs(Map.Top) + Math.Abs(Map.Bottom));
        }
        else
        {
            Map.Left = FootholdTree.MinX1.First();
            Map.Bottom = -Map.Info["centerY"] + Map.Info["MapHeight"] - 55;
            Map.SaveMapBottom = Map.Bottom - 55;
            Map.Top = -Map.Info["centerY"] + 50;
            Map.Right = FootholdTree.MaxX2.Last();
            Map.Info.AddOrReplace("MapWidth", Map.Right - Map.Left);
            EngineFunc.SpriteEngine.Camera.X = Map.Left;
            EngineFunc.SpriteEngine.Camera.Y = Map.Bottom;

        }

        Map.CreateResLoader();

        LadderRope.Create();
        MapTile.Create();

        Obj.Create();

        //Map.OffsetY = (DisplaySize.Y - 600) / 2;
        Back.Create();

        Particle.Create();
      
        if (!FirstLoaded)
        {
            string Name = Wz.GetNode("String/Mob.img/100100/name").ToStr();
            switch (Name)
            {
                case "Snail":
                    Wz.Region = "GMS";
                    Map.NpcNameTagFont = "Arial13";
                    Map.NpcBalloonFont = "Arial12";
                    Map.MobLvFont = "Arial10";
                    Map.ToolTipFont = "Arial14";
                    UseD2D = true;
                    break;
                case "달팽이":
                    Wz.Region = "KMS";
                    Map.NpcNameTagFont = "Arial12";
                    Map.NpcBalloonFont = "Arial12";
                    Map.MobLvFont = "Arial10";
                    Map.ToolTipFont = "Arial14";
                    UseD2D = true;
                    break;
                case "デンデン":
                    Wz.Region = "JMS";
                    Map.NpcNameTagFont = "MSGothic12";
                    Map.NpcBalloonFont = "Verdana11";
                    Map.MobLvFont = "Verdana9";
                    Map.ToolTipFont = "MSGothic14";
                    UseD2D = false;
                    break;
                default:
                    Wz.Region = "TMS";
                    Map.NpcNameTagFont = "SimSun13";
                    Map.NpcBalloonFont = "Verdana11";
                    Map.MobLvFont = "Verdana9";
                    Map.ToolTipFont = "SimSun14";
                    UseD2D = false;
                    break;
                  
            }
            Player.SpawnNew();
            NameTag.Create("SuperGM");
            GameCursor.LoadRes("0");
            if (Wz.HasNode("UI/Basic.img/Cursor/12"))
                GameCursor.LoadRes("12");
            else
                GameCursor.IsDataWz = true;
            MiniMap = new MiniMap();
            UI.ControlManager.Controls.Add(MiniMap);
            if(Wz.HasNode("UI/UIWindow4.img"))
                MiniMap.Version=3;
            else
                MiniMap.Version=1;
            FirstLoaded = true;
        }

        MiniMap.ReDraw();
        Npc.Create();
        Mob.Create();
        ObjToolTip.Create();

        Skill.PlayEnded = true;

        Map.OffsetY = (Map.DisplaySize.Y - 600) / 2;
        Back.ResetPos = true;
        Particle.ResetPos = true;
        EngineFunc.SpriteEngine.Move(1);

        BgmName = Map.Img.GetStr("info/bgm");
        BgmList.Add(BgmName);
        if (BgmList.Count > 2)
            BgmList.RemoveAt(0);
        if (BgmList.Count > 1)
        {
            if (BgmName == BgmList[0])
                return;
        }

        if (BgmName.Contains(".img"))
        {
            Music.Play("Sound/" + BgmName);
        }
        else
        {
            var Split = BgmName.Split('/');
            string BgmPath = "Sound/" + Split[0] + ".img/" + Split[1];
            Music.Play(BgmPath);
        }
        // if( EngineFunc.SpriteEngine.SpriteList==null)
    }

}

