using ProtoBuf;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace dmplayer.src
{
    static class Util
    { 
        public class SaveData<T> where T : new()
        {
            /*
             * Handle the creation of custom savedata
             * Save is executed on game save
             */
            ISaveGame save;

            // Data that we modify
            public T data;
            public string key;


            public SaveData(ICoreServerAPI api, string key)
            {
                this.save = api.WorldManager.SaveGame;
                this.key = key;

                data = (HasSaveData(save, key)) ?
                    GetSaveData<T>(save, key) :
                    new T();

                api.Event.GameWorldSave += Save;
            }

            private void Save()
            {
                if (key == null || save == null)
                {
                    throw new Exception("SaveData not initialized");
                }
                SetSaveData<T>(save, key, data);
            }
        }

        //Utilities, move these out of scope
        static byte[] ValToByteArr<T>(T val)
        {
            // unsure whether this is the best way about it
            MemoryStream stream = new MemoryStream();
            Serializer.Serialize<T>(stream, val);

            return stream.ToArray();
        }

        static T ByteArrToVal<T>(byte[] arr)
        {
            MemoryStream stream = new MemoryStream(arr);
            return Serializer.Deserialize<T>(stream);
        }
        //Utilities end

        //Cleaner looking methods
        public static bool HasSaveData(ISaveGame save, string key)
        {
            return (save.GetData(key) != null);
        }
        public static T GetSaveData<T>(ISaveGame save, string key)
        {
            return ByteArrToVal<T>(save.GetData(key));
        }
        public static void SetSaveData<T>(ISaveGame save, string key, T data)
        {
            save.StoreData(key, ValToByteArr(data));
        }

        public static double ToGameTime(IGameCalendar calendar, double hours)
        {
            return (calendar.SpeedOfTime * calendar.CalendarSpeedMul) * hours;
        }
    
        public static double FromGameTime(IGameCalendar calendar, double gameHours)
        {
            return (gameHours / (calendar.SpeedOfTime * calendar.CalendarSpeedMul));
        }
    }
}
