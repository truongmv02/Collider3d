using UnityEngine;

namespace TMV.Base
{
    public  class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance != null) return instance;
                
                instance = FindObjectOfType<T>();
                if (instance != null) return instance;
                
                var singletonObject = new GameObject(typeof(T).Name);
                instance = singletonObject.AddComponent<T>();

                return instance;
            }
        }

        protected virtual void Awake()
        {
            CreateInstance();
        }

        protected virtual void CreateInstance(bool destroyInLoad = true)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this as T;
                if (!destroyInLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
        }
    }
}