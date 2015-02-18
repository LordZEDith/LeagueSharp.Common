﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LeagueSharp.Common
{
    [Obsolete(
        "This will most likely not be needed anymore when Jodus adds it to LeagueSharp.dll," +
        "only use it if you know what you are doing!", false)]
    public class ObjectHandler
    {
        private static readonly Dictionary<Type, Dictionary<int, GameObject>> gameObjects =
            new Dictionary<Type, Dictionary<int, GameObject>>();

        static ObjectHandler()
        {
            var i = 0;
            // All existing objects
            foreach (var obj in ObjectManager.Get<GameObject>())
            {
                var type = obj.GetType();
                if (!gameObjects.ContainsKey(type))
                {
                    gameObjects.Add(type, new Dictionary<int, GameObject>());
                }

                var index = obj.NetworkId;
                if (index == 0)
                {
                    index = i;
                    i++;
                }

                gameObjects[type][index] = obj;
            }

            // Listen to events
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            GameObject.OnDelete += Obj_AI_Base_OnDelete;
        }

        public static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            var type = sender.GetType();
            if (!gameObjects.ContainsKey(type))
            {
                gameObjects.Add(type, new Dictionary<int, GameObject>());
            }

            gameObjects[type][sender.NetworkId] = sender;
        }

        private static void Obj_AI_Base_OnDelete(GameObject sender, EventArgs args)
        {
            foreach (var dictionary in gameObjects.Values)
            {
                dictionary.Remove(sender.NetworkId);
            }
        }

        public static GameObjectWrapper<T> Get<T>() where T : GameObject, new()
        {
            var type = typeof(T);
            var found = new GameObjectWrapper<T>();

            foreach (var key in gameObjects.Keys.FindAll(key => type.IsAssignableFrom(key)))
            {
                found.AddRange(gameObjects[key].Values.FindAll(o => o.IsValid<T>()).ConvertAll(o => (T) o));
            }

            return found;
        }

        public static T GetUnitByNetworkId<T>(int networkId) where T : GameObject, new()
        {
            return
                (from dict in gameObjects.Values where dict.ContainsKey(networkId) select (T) dict[networkId])
                    .FirstOrDefault(o => o.IsValid<T>());
        }

        public class GameObjectWrapper<T> : List<T> where T : GameObject, new()
        {
            public List<T> Allies
            {
                get { return FindAll(o => o.IsValid<T>() && o.IsAlly); }
            }

            public List<T> Enemies
            {
                get { return FindAll(o => o.IsValid<T>() && o.IsEnemy); }
            }

            public List<T> Neutrals
            {
                get { return FindAll(o => o.IsValid<T>() && o.Team == GameObjectTeam.Neutral); }
            }
        }
    }
}