using UnityEngine;

namespace TMV.Base
{
    public  class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                
                _instance = FindObjectOfType<T>();
                if (_instance != null) return _instance;
                
                var singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            CreateInstance();
        }

        protected virtual void CreateInstance(bool destroyInLoad = true)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this as T;
                if (!destroyInLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
        }
    }
}