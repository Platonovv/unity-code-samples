using System;
using UnityEngine;

namespace YagaPotions.Services.Platform
{
    public sealed class PlatformBridge : MonoBehaviour
    {
        private const char Separator = '|';

        public event Action<string> OnInitialized;
        public event Action<PauseReason> OnPaused;
        public event Action<PauseReason> OnResumed;
        public event Action<int, bool, string> OnResult;

        public void ReceiveInit(string info) => OnInitialized?.Invoke(info);

        public void ReceivePause(string source) => OnPaused?.Invoke(ParseReason(source));

        public void ReceiveResume(string source) => OnResumed?.Invoke(ParseReason(source));

        public void ReceiveResult(string message)
        {
            string[] parts = message.Split(Separator, 3);
            OnResult?.Invoke(int.Parse(parts[0]), parts[1] == "1", parts[2]);
        }

        private PauseReason ParseReason(string source) => source switch
        {
            "hidden" => PauseReason.Hidden,
            "muted" => PauseReason.Muted,
            _ => PauseReason.Platform
        };
    }
}
