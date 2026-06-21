using System.Threading;
using Battle.Presentation;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;

namespace Battle.Effects
{
    public interface IAsyncCardEffect
    {
        UniTask<bool> ApplyAsync(
            SkillPresentationPlaybackContext context,
            int finalValue,
            CancellationToken ct,
            EventChannelSO eventChannel);
        // true = 정상 완료, false = Fizzle 요청
    }
}
