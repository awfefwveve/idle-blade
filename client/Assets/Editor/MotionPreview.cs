using UnityEditor;
using UnityEngine;
namespace IdleBlade.Editor
{
    public sealed class MotionPreview:EditorWindow
    {
        string validation="";
        GameObject preview;int motion=1;bool paused;float frozen;double started;
        [MenuItem("IdleBlade/Motion Preview")]
        static void Open(){GetWindow<MotionPreview>("Motion Preview").Show();}
        void OnEnable(){started=EditorApplication.timeSinceStartup;}
        [MenuItem("IdleBlade/Validate Motion Grounding")]
        static void ValidateGrounding(){Debug.Log(BarbarianLobby.ValidateGroundContact());}
        void OnDisable(){if(preview!=null)DestroyImmediate(preview);}
        void OnGUI()
        {
            motion=GUILayout.Toolbar(motion,new[]{"대기","달리기","공격","피격","분할 / 폭발","관절 진단"});
            bool next=GUILayout.Toggle(paused,"일시정지");
            float now=(float)(EditorApplication.timeSinceStartup-started);
            if(next&&!paused)frozen=now;if(!next&&paused)started=EditorApplication.timeSinceStartup-frozen;paused=next;
            GUILayout.BeginHorizontal();
            foreach(float sample in new[]{.08f,.16f,.30f,.55f,.80f})if(GUILayout.Button(sample.ToString("0.00")+"초",GUILayout.Width(90))){paused=true;frozen=sample;}
            if(GUILayout.Button("접지 전체 검사",GUILayout.Width(150))){try{validation=BarbarianLobby.ValidateGroundContact();Debug.Log(validation);}catch(System.Exception e){validation=e.Message;Debug.LogError(validation);}}
            GUILayout.Label(validation);
            GUILayout.EndHorizontal();
            if(preview==null){preview=new GameObject("Motion preview - no network"){hideFlags=HideFlags.HideAndDontSave};preview.AddComponent<BarbarianLobby>().enabled=false;}
            preview.GetComponent<BarbarianLobby>().RenderMotionPreview(position.width,position.height-75,paused?frozen:now,motion);
        }
        void Update(){Repaint();}
    }
}
