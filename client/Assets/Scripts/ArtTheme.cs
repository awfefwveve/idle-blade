using UnityEngine;

namespace IdleBlade
{
    [CreateAssetMenu(menuName = "Barbarian/Art Theme")]
    public sealed class ArtTheme : ScriptableObject
    {
        [Tooltip("Transparent 128px square cells, horizontal strip; feet at (64,112), facing right.")]
        public Texture2D heroIdle, heroAttack, enemyIdle, enemyHit;
        [Tooltip("1280x432, no UI, horizon y=220; feet line y=343 (from top).")]
        public Texture2D battlefield;
        public Font uiFont;
        [Range(1, 16)] public int framesPerSecond = 8;
    }
}
