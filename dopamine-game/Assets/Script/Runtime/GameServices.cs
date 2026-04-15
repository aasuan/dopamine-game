using UnityEngine;

namespace DopamineGame.Runtime
{
    public interface IAudioService
    {
        void PlayCue(string cueId, Vector3 worldPosition);
    }

    public interface IMenuService
    {
        void TogglePauseMenu();
    }

    public readonly struct GameServices
    {
        public GameServices(IAudioService audio, IMenuService menu)
        {
            Audio = audio;
            Menu = menu;
        }

        public IAudioService Audio { get; }

        public IMenuService Menu { get; }
    }

    public sealed class NullAudioService : IAudioService
    {
        public void PlayCue(string cueId, Vector3 worldPosition)
        {
        }
    }

    public sealed class NullMenuService : IMenuService
    {
        public void TogglePauseMenu()
        {
        }
    }
}
