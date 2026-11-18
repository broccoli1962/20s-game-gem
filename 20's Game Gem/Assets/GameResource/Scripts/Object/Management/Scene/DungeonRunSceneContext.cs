using Cysharp.Threading.Tasks;
using DdaIT.DungeonRun;
using UnityEngine;

namespace DdaIT.Object.Management
{
    public sealed class DungeonRunSceneContext : SceneContext
    {
        private GameObject _run;

        /// <summary>Prepares the dungeon audio before the start button becomes available.</summary>
        public override UniTask PreloadAsync() => AudioManager.PreloadClipsAsync(
            "CryptEnter", "CryptMachinery", "CryptStrike", "CryptCharge", "CryptStagger",
            "CryptFrenzy", "CryptFrenzyStrike", "CryptComboFade", "CryptFinish");

        /// <summary>Creates the round controller and its scene-owned presentation.</summary>
        public override UniTask EnterAsync()
        {
            _run = new GameObject("Dungeon Run");
            _run.AddComponent<DungeonRunController>();
            return UniTask.CompletedTask;
        }

        /// <summary>Removes the round before leaving its scene.</summary>
        public override UniTask ExitAsync()
        {
            if (_run != null)
            {
                AudioManager.StopBgm();
                UnityEngine.Object.Destroy(_run);
            }
            return UniTask.CompletedTask;
        }
    }
}
