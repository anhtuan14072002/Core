using System;

namespace Core
{
    [Serializable]
    public partial class UserData
    {
        public int Version;
        public const string Key = "Version";

        public int LoadVersion()
        {
            return ES3.Load(Key, Version);
        }

        public void SaveVersion()
        {
            ES3.Save(Key, Version);
        }
    }
}