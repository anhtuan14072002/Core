using System;
using System.Reflection;
using UnityEngine;

namespace Core
{
    public class GameData
    {
        private const string ResourcePath = "UserDataDefault";
        public readonly UserData User;

        public GameData()
        {
            var userDataDefault = Resources.Load<UserDataDefault>(ResourcePath);
            User = userDataDefault != null ? userDataDefault.UserData : new UserData();
            if (!ES3.FileExists())
            {
                SaveAll();
                return;
            }
            int oldVersion = User.LoadVersion();
            if (User.Version > oldVersion)
            {
                ES3.DeleteFile();
                SaveAll();
                return;
            }
            LoadAll();
        }

        private void LoadAll()
        {
            try
            {
                MethodInfo[] methods = User.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var method in methods)
                    if (method.GetCustomAttributes(typeof(LoadDataAttribute), false).Length > 0)
                        method.Invoke(User, null);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void SaveAll()
        {
            try
            {
                MethodInfo[] methods = User.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var method in methods)
                    if (method.GetCustomAttributes(typeof(SaveDataAttribute), false).Length > 0)
                        method.Invoke(User, null);
            }
            catch (System.IO.IOException e)
            {
                Debug.LogError(e);
            }
            catch (System.Security.SecurityException e)
            {
                Debug.LogError(e);
            }
            catch (FormatException e)
            {
                Debug.LogError(e);
                ES3.DeleteFile();
                SaveAll();
            }
        }
    }
}