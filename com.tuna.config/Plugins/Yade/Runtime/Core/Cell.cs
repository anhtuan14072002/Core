//  Copyright (c) 2020-present amlovey
//  
using System;
using UnityEngine;

namespace Yade.Runtime
{
    /// <summary>
    /// Data clas for cell of sheet
    /// </summary>
    [Serializable]
    public class Cell
    {
        [SerializeField]
        private string rawValue;
        [SerializeField]
        private string value;
        [SerializeField]
        private UnityEngine.Object UnityObject;
        [SerializeField]
        private UnityEngine.Object[] UOs;

        public Cell(string rawValue = "")
        {
            this.rawValue = rawValue;
        }

        /// <summary>
        /// Get value of the cell. Normally if the cell is formula cell, it will be 
        /// result of the formula. But if the cell contains Unity object, it will be 
        /// the type name of the Unity object. For cell has Unity object, use the 
        /// GetUnityObject method
        /// </summary>
        /// <returns>Value of cell</returns>
        public string GetValue()
        {
            return value;
        }

        internal void SetValue(string value)
        {
            this.value = value;
        }

        /// <summary>
        /// Get raw value of the cell
        /// </summary>
        /// <returns></returns>
        public string GetRawValue()
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return value;
            }

            if (rawValue.StartsWith("="))
            {
                return rawValue;
            }

            return value;
        }

        /// <summary>
        /// Whether the cell contains formula or not
        /// </summary>
        /// <returns>True for cell continas formula, False for cell does not contains formula</returns>
        public bool IsFormulaCell()
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

            return rawValue.Trim().StartsWith("=");
        }

        /// <summary>
        /// Whether the cell has Unity object or not
        /// </summary>
        /// <returns>True for cell contains Unity object, False for not contains Unity object</returns>
        public bool HasUnityObject()
        {
            if (UnityObject != null)
            {
                return true;
            }
            
            if (UOs != null)
            {
                foreach (var item in UOs)
                {
                    if (item != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get the unity object of the cell
        /// </summary>
        /// <returns>Unity object value of this cell</returns>
        public UnityEngine.Object GetUnityObject()
        {
            return UnityObject as UnityEngine.Object;
        }

        /// <summary>
        /// Get the unity objects of the cell
        /// </summary>
        /// <returns>Unity objects value of this cell</returns>
        public UnityEngine.Object[] GetUnityObjects()
        {
            return UOs;
        }

        /// <summary>
        /// Get the unity object of the cell
        /// </summary>
        /// <returns>Unity object value of this cell</returns>
        public T GetUnityObject<T>() where T: UnityEngine.Object
        {
            return UnityObject as T;
        }

        /// <summary>
        /// Get the unity objects of the cell
        /// </summary>
        /// <returns>Unity objects value of this cell</returns>
        public T[] GetUnityObjects<T>() where T: UnityEngine.Object
        {
            var objects = UOs as UnityEngine.Object[];
            if (objects == null)
            {
                return null;
            }

            var ret = new T[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                ret[i] = objects[i] as T;
            }

            return ret;
        }

        internal void SetUnityObject(UnityEngine.Object obj)
        {
            this.UnityObject = obj;
        }

        internal void SetUnityObjects(UnityEngine.Object[] objs)
        {
            this.UOs = objs;
        }

        internal void SetRawValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.StartsWith("="))
                {
                    rawValue = value;
                    return;
                }

                this.value = value;
                rawValue = string.Empty;
                return;
            }
            
            rawValue = value;
        }
    }
    

    /// <summary>
    /// Indexes of cell
    /// </summary>
    public class CellIndex
    {
        /// <summary>
        /// Row index of this cell
        /// </summary>
        public int row;

        /// <summary>
        /// Column index of this cell
        /// </summary>
        public int column;

        public CellIndex()
        {
            row = -1;
            column = -1;
        }
    }
}