using UnityEngine;
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public enum InitializationTiming
        {
            Awake,
            Start,
            LateStart
        }

        [SerializeField]
        private InitializationTiming initializeOn = InitializationTiming.Awake;

        public static T Instance { get; protected set; }

        protected virtual void Awake()
        {
            if (initializeOn == InitializationTiming.Awake)
            {
                TryInitialize();
            }
        }

        protected virtual void Start()
        {
            if (initializeOn == InitializationTiming.Start)
            {
                TryInitialize();
            }
        }

        private void LateUpdate()
        {
            if (initializeOn == InitializationTiming.LateStart)
            {
                TryInitialize();
            }
        }

        private void TryInitialize()
        {
            if (Instance == null)
            {
                Instance = GetComponent<T>();
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

