using System;
using System.Collections.Generic;
using System.Linq;
using Tetris;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Tetris
{
    public class DisposeGameObjects : IDisposable
    {
        private readonly List<Object> objs;

        private DisposeGameObjects(params Object[] objs)
        {
            this.objs = objs.ToList();
        }

        public void Add(Object obj)
        {
            objs.Add(obj);
        }

        public void Dispose()
        {
            foreach (var obj in objs.Where(obj => obj != null))
            {
                if (obj is Component component)
                {
                    ObjectHelpers.Destroy(component.gameObject);
                }
                else
                {
                    ObjectHelpers.Destroy(obj);
                }
            }
        }

        public static DisposeGameObjects Of(params Object[] objs)
        {
            return new DisposeGameObjects(objs);
        }
    }
}