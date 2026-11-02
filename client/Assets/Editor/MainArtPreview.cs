using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace IdleBlade.Editor
{
    public sealed class MainArtPreview : EditorWindow
    {
        GameObject preview;
        [MenuItem("IdleBlade/Art Preview")]
        static void Open(){GetWindow<MainArtPreview>("Art Preview").Show();}
        void OnEnable()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            preview=new GameObject("Art Preview - no network"){hideFlags=HideFlags.HideAndDontSave};
            var game=preview.AddComponent<IdleGame>();game.enabled=false;
            var sample=new IdleGame.Snapshot{damage=120,requiredKills=15,enemyMaxHp=200,
                state=new IdleGame.State{stage=1,level=1,kills=7,gold=12500,monsters=new[]{
                    new IdleGame.Monster{id=1,x=1800,hp=120,maxHp=200},
                    new IdleGame.Monster{id=2,x=2800,hp=200,maxHp=200},
                    new IdleGame.Monster{id=3,x=3900,hp=200,maxHp=200}}}};
            sample.state.monsters=new IdleGame.Monster[24];
            for(int i=0;i<24;i++)sample.state.monsters[i]=new IdleGame.Monster{id=i+1,sector=i%8,x=950+(i/8)*1500,hp=120,maxHp=200};
            typeof(IdleGame).GetField("snapshot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,sample);
            preview.AddComponent<BarbarianLobby>().enabled=false;
        }
        void OnDisable(){if(preview!=null)DestroyImmediate(preview);}
        void OnGUI()
        {
            if(Application.isPlaying){EditorGUILayout.HelpBox("Stop Play mode to inspect the read-only art preview.",MessageType.Info);return;}
            if(preview==null)OnEnable();
            if(preview!=null)preview.GetComponent<BarbarianLobby>().RenderArtPreview(position.width,position.height);
        }
        void Update(){Repaint();}
    }
}
