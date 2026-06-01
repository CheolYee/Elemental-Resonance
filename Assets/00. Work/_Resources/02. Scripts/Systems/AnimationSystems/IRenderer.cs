namespace _00._Work._Resources._02._Scripts.Systems.AnimationSystems
{
    public interface IRenderer
    { 
        void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0);
    }
}