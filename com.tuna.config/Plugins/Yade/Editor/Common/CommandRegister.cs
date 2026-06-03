//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Yade.Editor
{
    public class KeyMod
    {
        public const int Shift = 512;
        public const int Ctrl = 1024;
        public const int Alt = 2048;
        public const int Cmd = 4096;
    }

    public class CommandRegister
    {
        private Dictionary<int, Action> maps;

        public CommandRegister()
        {
            maps = new Dictionary<int, Action>();
        }

        public void Register(int windowShortcut, int macShortcut, Action action)
        {
            int code = windowShortcut;
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                code = macShortcut;
            }

            Register(code, action);
        }

        public void Register(int shortcut, Action action)
        {
            if (maps.ContainsKey(shortcut))
            {
                maps[shortcut] = action;
            }
            else
            {
                maps.Add(shortcut, action);
            }
        }

        public bool Execute(KeyDownEvent evt)
        {
            int code = GetCode(evt);
            if (maps.ContainsKey(code))
            {
                maps[code].Invoke();
                return true;
            }

            return false;
        }

        private int GetCode(KeyDownEvent evt)
        {
            int code = (int)evt.keyCode;
            if (evt.shiftKey)
            {
                code = KeyMod.Shift | code;
            }

            if (evt.ctrlKey)
            {
                code = KeyMod.Ctrl | code;
            }

            if (evt.commandKey)
            {
                code = KeyMod.Cmd | code;
            }

            if (evt.altKey)
            {
                code = KeyMod.Alt | code;
            }

            return code;
        }
    }
}
