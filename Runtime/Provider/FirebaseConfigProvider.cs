using System;
using System.Linq;
using System.Threading.Tasks;
using com.ktgame.config.core;
using UnityEngine;

#if FIREBASE_REMOTE_CONFIG

using Firebase.Extensions;
using Firebase.RemoteConfig;

namespace com.ktgame.services.remote_config.provider
{
    public class FirebaseConfigProvider : IConfigProvider
    {
        public event Action OnFetchSuccess;
        public event Action OnFetchError;
        public event Action OnSetDefaultComplete;

        private readonly IConfigCache _cache;
        private IConfigBlueprint _defaultConfig;

        public FirebaseConfigProvider(IConfigCache cache)
        {
            _cache = cache;
        }

        public IConfigValue GetValue(string id)
        {
            return new FirebaseConfigValue(id);
        }

        public void SetDefaultValues(IConfigBlueprint config)
        {
            _cache.Load(config);
            _defaultConfig = config;
            FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(config.Export()).ContinueWithOnMainThread(SetDefaultCompleteHandler);
        }

        public void Fetch()
        {
            FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(FetchCompleteHandler);
        }

        private void SetDefaultCompleteHandler(Task task)
        {
            if (task.IsCompleted)
            {
                OnSetDefaultComplete?.Invoke();
            }
        }

        private async void FetchCompleteHandler(Task fetchTask)
        {
            ConfigInfo info = null;
            try
            {
                info = FirebaseRemoteConfig.DefaultInstance.Info;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.LogWarning("RemoteConfig.Info threw exception: " + ex);
            }

            if (info != null)
            {
                switch (info.LastFetchStatus)
                {
                    case LastFetchStatus.Success:
                        await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                        UpdateDefaultConfigs();
                        OnFetchSuccess?.Invoke();
                        break;
                    case LastFetchStatus.Failure:
                        OnFetchError?.Invoke();
                        break;
                    case LastFetchStatus.Pending:
                        OnFetchError?.Invoke();
                        break;
                    default:
                        OnFetchError?.Invoke();
                        break;
                }
            }
        }

        private void UpdateDefaultConfigs()
        {
            foreach (var key in _defaultConfig.StringKeys.ToList())
            {
                _defaultConfig.SetString(key, GetValue(key).String);
            }

            foreach (var key in _defaultConfig.IntKeys.ToList())
            {
                _defaultConfig.SetInt(key, GetValue(key).Int);
            }

            foreach (var key in _defaultConfig.FloatKeys.ToList())
            {
                _defaultConfig.SetFloat(key, GetValue(key).Float);
            }

            foreach (var key in _defaultConfig.BoolKeys.ToList())
            {
                _defaultConfig.SetBool(key, GetValue(key).Boolean);
            }

            _cache.Cache(_defaultConfig);
        }
    }
}
#endif