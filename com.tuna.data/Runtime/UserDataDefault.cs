using System;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "UserDataDefault", menuName = "Game/Data", order = 0)]
    public class UserDataDefault : ScriptableObject
    {
        public UserData UserData;
    }
}