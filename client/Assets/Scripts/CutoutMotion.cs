using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        Texture2D heroParts,goblinParts;
        System.Collections.Generic.List<RigFragment> captureParts;
        Matrix4x4 captureBase;
        bool captureOnly;
        Vector4 activeBoneClip;
        Vector2 activeBoneRange;
        readonly System.Collections.Generic.List<RigFragment> composedPose=new System.Collections.Generic.List<RigFragment>(24);
        Material rigMaterial;
        void DrawComposedPose(System.Collections.Generic.List<RigFragment> parts)
        {
            if(parts.Count==0)return;
            if(rigMaterial==null){var shader=Resources.Load<Shader>("Art/Rigs/RigSprite");if(shader==null)return;rigMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};}
            var bounds=new Rect(float.MaxValue,float.MaxValue,0,0);float right=float.MinValue,bottom=float.MinValue;
            foreach(var p in parts)for(int corner=0;corner<4;corner++) {
                var q=new Vector2((corner==1||corner==2)?p.target.xMax:p.target.x,corner>=2?p.target.yMax:p.target.y);
                Vector2 v=p.matrix.MultiplyPoint3x4(q);bounds.x=Mathf.Min(bounds.x,v.x);bounds.y=Mathf.Min(bounds.y,v.y);right=Mathf.Max(right,v.x);bottom=Mathf.Max(bottom,v.y);
            }
            bounds.width=right-bounds.x;bounds.height=bottom-bounds.y;
            if(Event.current.type!=EventType.Repaint)return;
            var rt=RenderTexture.GetTemporary(512,512,0,RenderTextureFormat.ARGB32);rt.filterMode=FilterMode.Point;
            var previous=RenderTexture.active;var tint=GUI.color;
            try {
            RenderTexture.active=rt;GL.Clear(true,true,Color.clear);GL.PushMatrix();GL.LoadPixelMatrix(bounds.x,bounds.xMax,bounds.yMax,bounds.y);
            foreach(var p in parts) {
                rigMaterial.mainTexture=p.texture;rigMaterial.SetVector("_BoneClip",p.boneClip);rigMaterial.SetVector("_BoneRange",p.boneRange);rigMaterial.SetPass(0);GL.Begin(GL.QUADS);GL.Color(Color.white);
                GL.TexCoord2(p.source.x/1280,1-p.source.y/1280);GL.Vertex(p.matrix.MultiplyPoint3x4(new Vector3(p.target.x,p.target.y,0)));
                GL.TexCoord2(p.source.xMax/1280,1-p.source.y/1280);GL.Vertex(p.matrix.MultiplyPoint3x4(new Vector3(p.target.xMax,p.target.y,0)));
                GL.TexCoord2(p.source.xMax/1280,1-p.source.yMax/1280);GL.Vertex(p.matrix.MultiplyPoint3x4(new Vector3(p.target.xMax,p.target.yMax,0)));
                GL.TexCoord2(p.source.x/1280,1-p.source.yMax/1280);GL.Vertex(p.matrix.MultiplyPoint3x4(new Vector3(p.target.x,p.target.yMax,0)));GL.End();
            }
            GL.PopMatrix();RenderTexture.active=previous;GUI.color=tint;GUI.DrawTexture(bounds,rt,ScaleMode.StretchToFill,true);
            } finally {RenderTexture.active=previous;GUI.color=tint;RenderTexture.ReleaseTemporary(rt);}
        }
        void OnDestroy(){if(rigMaterial!=null){if(Application.isPlaying)Destroy(rigMaterial);else DestroyImmediate(rigMaterial);}}
        float heroAttackAt=-10,heroHurtUntil;
        bool heroWasSwinging;
        double rigPreviousHp=-1;
        struct BoneArt
        {
            public Rect crop; public Vector2 from,to;
            public BoneArt(float x,float y,float w,float h,float ax,float ay,float bx,float by)
            {crop=new Rect(x,y,w,h);from=new Vector2(ax,ay);to=new Vector2(bx,by);}
        }
        // Pixel coordinates are authored against the 1280 atlas, independent of import resolution.
        static readonly BoneArt[] heroBones={
            new BoneArt(70,545,180,240,181,574,171,735),new BoneArt(380,553,205,237,446,583,526,737),
            new BoneArt(695,545,185,238,787,570,791,729),new BoneArt(1000,553,220,237,1077,583,1152,736),
            new BoneArt(75,889,190,292,180,911,143,1126),new BoneArt(390,914,190,267,475,943,482,1128),
            new BoneArt(695,889,200,305,790,911,819,1130),new BoneArt(990,914,250,260,1074,943,1100,1128)};
        static readonly BoneArt[] goblinBones={
            new BoneArt(48,563,236,280,208,646,112,757),new BoneArt(348,620,230,230,509,692,412,785),
            new BoneArt(680,561,211,284,755,641,827,751),new BoneArt(996,620,225,230,1071,701,1160,787),
            new BoneArt(46,862,243,240,217,932,119,1032),new BoneArt(337,870,242,300,451,954,486,1102),
            new BoneArt(682,871,218,258,751,948,828,1047),new BoneArt(997,883,218,278,1090,965,1104,1104)};
        void Atlas(Texture2D texture,Rect source,Rect target)
        {
            if(captureParts!=null)captureParts.Add(new RigFragment{texture=texture,source=source,target=target,matrix=captureBase*GUI.matrix,boneClip=activeBoneClip,boneRange=activeBoneRange});
            if(!captureOnly)DrawRigTexture(texture,source,target);
        }
        void AtlasRegion(Texture2D texture,Rect whole,Rect region,Rect target)
        {
            Atlas(texture,region,new Rect(target.x+(region.x-whole.x)/whole.width*target.width,
                target.y+(region.y-whole.y)/whole.height*target.height,region.width/whole.width*target.width,region.height/whole.height*target.height));
        }
        static void DrawRigTexture(Texture2D texture,Rect source,Rect target)
        {
            // Flatten the model transform before IMGUI clips rectangles with negative local coordinates.
            var saved=GUI.matrix;
            Vector2 a=saved.MultiplyPoint3x4(new Vector3(target.x,target.y,0));
            Vector2 b=saved.MultiplyPoint3x4(new Vector3(target.xMax,target.y,0));
            Vector2 c=saved.MultiplyPoint3x4(new Vector3(target.x,target.yMax,0));
            Vector2 right=b-a,down=c-a;
            bool mirrored=right.x*down.y-right.y*down.x<0;
            if(mirrored){a=b;right=-right;}
            float angle=Mathf.Atan2(right.y,right.x)*Mathf.Rad2Deg;
            GUI.matrix=Matrix4x4.TRS(a,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(-a);
            GUI.DrawTextureWithTexCoords(new Rect(a.x,a.y,right.magnitude,down.magnitude),texture,
                new Rect((mirrored?source.xMax:source.x)/1280,1-source.yMax/1280,(mirrored?-source.width:source.width)/1280,source.height/1280));
            GUI.matrix=saved;
        }
        void Bone(Texture2D texture,BoneArt art,Vector2 from,Vector2 to,bool distal=false)
        {
            Vector2 a=art.to-art.from,b=to-from;
            float scale=b.magnitude/a.magnitude;
            float angle=(Mathf.Atan2(b.y,b.x)-Mathf.Atan2(a.y,a.x))*Mathf.Rad2Deg;
            var saved=GUI.matrix;
            RotateRig(angle,from);
            activeBoneRange=new Vector2(-.03f,distal?3:1.03f);
            activeBoneClip=new Vector4(art.from.x/1280,1-art.from.y/1280,a.x/1280,-a.y/1280);
            Atlas(texture,art.crop,new Rect(from.x+(art.crop.x-art.from.x)*scale,from.y+(art.crop.y-art.from.y)*scale,art.crop.width*scale,art.crop.height*scale));
            activeBoneClip=Vector4.zero;GUI.matrix=saved;
        }
        static void RotateRig(float angle,Vector2 pivot)
        {GUI.matrix=GUI.matrix*Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-pivot);}
        static Vector2 Joint(Vector2 parent,float length,float angle)
        {return parent+new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad))*length;}
        // Opaque silhouette hulls measured from the original atlases; textures stay non-readable.
        static readonly Vector2[] heroSole0={new Vector2(404.211f,1137.097f),new Vector2(407.273f,959.490f),new Vector2(412.376f,934.992f),new Vector2(424.625f,918.660f),new Vector2(429.729f,913.557f),new Vector2(548.134f,913.557f),new Vector2(560.383f,923.764f),new Vector2(572.632f,934.992f),new Vector2(573.652f,936.013f),new Vector2(577.735f,941.116f),new Vector2(578.756f,961.531f),new Vector2(578.756f,976.842f),new Vector2(554.258f,1177.927f),new Vector2(553.238f,1178.947f),new Vector2(549.155f,1180.989f),new Vector2(425.646f,1180.989f),new Vector2(421.563f,1178.947f),new Vector2(420.542f,1177.927f),new Vector2(412.376f,1169.761f),new Vector2(404.211f,1161.595f)};
        static readonly Vector2[] heroSole1={new Vector2(1021.754f,954.386f),new Vector2(1028.900f,932.951f),new Vector2(1044.211f,917.640f),new Vector2(1049.314f,913.557f),new Vector2(1154.450f,913.557f),new Vector2(1167.719f,923.764f),new Vector2(1177.927f,931.930f),new Vector2(1178.947f,932.951f),new Vector2(1186.093f,941.116f),new Vector2(1240.191f,1124.848f),new Vector2(1240.191f,1173.844f),new Vector2(1138.118f,1173.844f),new Vector2(1060.542f,1166.699f),new Vector2(1055.439f,1163.636f),new Vector2(1053.397f,1161.595f),new Vector2(1050.335f,1157.512f),new Vector2(1048.293f,1153.429f),new Vector2(1021.754f,965.614f)};
        static readonly Vector2[] goblinSole0={new Vector2(365.423f,1153.429f),new Vector2(399.107f,933.971f),new Vector2(404.211f,926.826f),new Vector2(408.293f,921.722f),new Vector2(416.459f,914.577f),new Vector2(423.604f,910.494f),new Vector2(465.455f,910.494f),new Vector2(482.807f,921.722f),new Vector2(486.890f,924.785f),new Vector2(503.222f,941.116f),new Vector2(505.263f,943.158f),new Vector2(506.284f,944.179f),new Vector2(516.491f,959.490f),new Vector2(521.595f,969.697f),new Vector2(571.611f,1086.061f),new Vector2(578.756f,1105.455f),new Vector2(578.756f,1133.014f),new Vector2(575.694f,1136.077f),new Vector2(567.528f,1144.242f),new Vector2(484.848f,1168.740f),new Vector2(439.936f,1169.761f),new Vector2(366.443f,1169.761f),new Vector2(365.423f,1168.740f)};
        static readonly Vector2[] goblinSole1={new Vector2(1029.920f,1118.724f),new Vector2(1061.563f,963.573f),new Vector2(1065.646f,953.365f),new Vector2(1066.667f,951.324f),new Vector2(1069.729f,945.199f),new Vector2(1070.750f,943.158f),new Vector2(1077.895f,933.971f),new Vector2(1086.061f,925.805f),new Vector2(1090.144f,921.722f),new Vector2(1094.226f,918.660f),new Vector2(1101.372f,914.577f),new Vector2(1104.434f,913.557f),new Vector2(1130.973f,913.557f),new Vector2(1139.139f,914.577f),new Vector2(1144.242f,917.640f),new Vector2(1149.346f,921.722f),new Vector2(1153.429f,925.805f),new Vector2(1161.595f,933.971f),new Vector2(1162.616f,934.992f),new Vector2(1209.569f,1148.325f),new Vector2(1209.569f,1160.574f),new Vector2(1089.123f,1160.574f),new Vector2(1047.273f,1140.159f),new Vector2(1045.231f,1139.139f),new Vector2(1035.024f,1131.994f),new Vector2(1030.941f,1127.911f),new Vector2(1029.920f,1119.745f)};
        static Vector2 StepFoot(Vector2 hip,float cycle,bool running,bool hero)
        {
            if(!running)return new Vector2(hip.x,0);
            float phase=Mathf.Repeat(cycle/(2*Mathf.PI),1),x,lift;
            if(phase<.6f) {x=Mathf.Lerp(18,-18,phase/.6f);lift=0;}
            else {float swing=(phase-.6f)/.4f;x=Mathf.SmoothStep(-18,18,swing);lift=Mathf.Sin(swing*Mathf.PI)*16;}
            return new Vector2(hip.x+x*(hero?1:-1),-lift);
        }
        static Vector2 LegKnee(Vector2 hip,Vector2 ankle,float bend,bool running)
        {
            Vector2 delta=ankle-hip;float d=delta.magnitude;
            float length=d*.5f+(running?Mathf.Clamp((-18-ankle.y)*.1f,.05f,1.2f):.05f);
            Vector2 along=delta.normalized;
            return hip+along*d*.5f+new Vector2(along.y,-along.x)*Mathf.Sqrt(Mathf.Max(0,length*length-d*d*.25f))*bend;
        }
        static Matrix4x4 BoneTransform(BoneArt art,Vector2 from,Vector2 to)
        {
            Vector2 a=art.to-art.from,b=to-from;
            float angle=(Mathf.Atan2(b.y,b.x)-Mathf.Atan2(a.y,a.x))*Mathf.Rad2Deg;
            return Matrix4x4.TRS(from,Quaternion.Euler(0,0,angle),Vector3.one*(b.magnitude/a.magnitude))*Matrix4x4.Translate(-art.from);
        }
        static float SoleY(BoneArt shin,Vector2 knee,Vector2 ankle,Vector2[] silhouette,Matrix4x4 body)
        {
            var transform=body*BoneTransform(shin,Vector2.LerpUnclamped(knee,ankle,-.15f),ankle);
            float bottom=float.MinValue;
            foreach(var p in silhouette)bottom=Mathf.Max(bottom,transform.MultiplyPoint3x4(p).y);
            return bottom;
        }
        static void SolveLeg(Vector2 hip,Vector2 sole,BoneArt shin,Vector2[] silhouette,Matrix4x4 body,bool hero,bool running,out Vector2 knee,out Vector2 ankle)
        {
            var inverse=body.inverse;
            ankle=inverse.MultiplyPoint3x4(sole+Vector2.up*-18);
            knee=LegKnee(hip,ankle,hero?1:-1,running);
            // Correct against the visible boot sole, not the ankle socket or transparent rectangle.
            for(int i=0;i<20;i++) {
                float error=sole.y-SoleY(shin,knee,ankle,silhouette,body);
                if(Mathf.Abs(error)<.01f)break;
                ankle+=(Vector2)inverse.MultiplyVector(new Vector3(0,error,0));
                knee=LegKnee(hip,ankle,hero?1:-1,running);
            }
        }
        bool DrawHeroRig(float x,float y,float height,bool running,bool swing)
        {
            if(heroParts==null)heroParts=Resources.Load<Texture2D>("Art/Rigs/hero-parts-v1");
            if(heroParts==null)return false;
            float now=Time.unscaledTime;
            if(swing&&(!heroWasSwinging||now-heroAttackAt>.42f))heroAttackAt=now;
            heroWasSwinging=swing;
            var s=game.Current;
            if(s!=null&&s.state.heroMaxHp>0) {
                if(rigPreviousHp>s.state.heroHp)heroHurtUntil=now+.2f;
                rigPreviousHp=s.state.heroHp;
            }
            DrawCutout(true,new Vector2(x,y),height/350,now,running,now-heroAttackAt,now<heroHurtUntil,false);
            return true;
        }
        bool DrawGoblinRig(EnemyView view,float size,int id,float distance,bool hurt,bool flip)
        {
            if(goblinParts==null)goblinParts=Resources.Load<Texture2D>("Art/Rigs/goblin-parts-v1");
            if(goblinParts==null)return false;
            // Normal mobs only receive damage on the server; do not fabricate attack/damage events.
            captureParts=view.pose;captureParts.Clear();captureBase=GUI.matrix.inverse;
            DrawCutout(false,view.foot,size*.82f/300,Time.unscaledTime+id*.173f,distance>1050,10,hurt,flip);
            captureParts=null;
            return true;
        }
        void DrawCutout(bool hero,Vector2 foot,float scale,float time,bool running,float attackAge,bool hurt,bool flip)
        {
            var texture=hero?heroParts:goblinParts;if(texture==null)return;
            var bones=hero?heroBones:goblinBones;
            float stride=running?Mathf.Sin(time*(hero?11:14)):0;
            float bounce=running?-Mathf.Abs(Mathf.Cos(time*(hero?11:14)))*3:Mathf.Sin(time*3)*1.4f;
            bool attack=attackAge>=0&&attackAge<.42f;
            float stroke=attack?attackAge<.1f?Mathf.SmoothStep(0,-55,attackAge/.1f):attackAge<.2f?Mathf.SmoothStep(-55,75,(attackAge-.1f)/.1f):Mathf.SmoothStep(75,0,(attackAge-.2f)/.22f):0;
            var original=GUI.matrix;var color=GUI.color;
            bool ownsCapture=captureParts==null,previousCaptureOnly=captureOnly;
            if(ownsCapture){captureParts=composedPose;captureParts.Clear();captureBase=original.inverse;}
            captureOnly=true;
            var placement=Matrix4x4.TRS(foot,Quaternion.identity,new Vector3(flip?-scale:scale,scale,1));
            float lean=hurt?-8:attack?stroke*.035f:running?(hero?4:-7):0;
            var body=Matrix4x4.Translate(new Vector3(0,bounce,0))*Matrix4x4.Translate(new Vector3(0,-110,0))*Matrix4x4.Rotate(Quaternion.Euler(0,0,lean))*Matrix4x4.Translate(new Vector3(0,110,0));
            GUI.matrix=original*placement*body;
            if(hurt)GUI.color=new Color(color.r,color.g*.7f,color.b*.65f,color.a);
            Vector2 hipBack=new Vector2(hero?-23:18,-111),hipFront=new Vector2(hero?23:-18,-111);
            // Feet travel along the ground during stance and lift only on the return step.
            float cycle=time*(hero?11:14);
            Vector2 kneeBack,ankleBack,kneeFront,ankleFront;
            SolveLeg(hipBack,StepFoot(hipBack,cycle,running,hero),bones[5],hero?heroSole0:goblinSole0,body,hero,running,out kneeBack,out ankleBack);
            SolveLeg(hipFront,StepFoot(hipFront,cycle+Mathf.PI,running,hero),bones[7],hero?heroSole1:goblinSole1,body,hero,running,out kneeFront,out ankleFront);
            Bone(texture,bones[4],hipBack,kneeBack);Bone(texture,bones[5],Vector2.LerpUnclamped(kneeBack,ankleBack,-.15f),ankleBack,true);
            Bone(texture,bones[6],hipFront,kneeFront);Bone(texture,bones[7],Vector2.LerpUnclamped(kneeFront,ankleFront,-.15f),ankleFront,true);
            Vector2 shoulderBack=new Vector2(hero?28:25,hero?-210:-205),shoulderFront=new Vector2(hero?-35:-28,hero?-210:-205);
            Vector2 elbowBack=Joint(shoulderBack,51,hero?25-stride*10+stroke:12+stride*18);
            Vector2 handBack=Joint(elbowBack,48,hero?65+stroke*.45f:-12);
            Bone(texture,bones[hero?2:0],shoulderBack,elbowBack);
            if(!hero)Bone(texture,bones[1],Vector2.LerpUnclamped(elbowBack,handBack,-.10f),handBack,true);
            float armAngle=hero?-8+stride*15:-18-stride*20+stroke;
            Vector2 elbowFront=Joint(shoulderFront,56,armAngle);
            Vector2 handFront=Joint(elbowFront,51,hero?10:armAngle-20);
            
            Bone(texture,bones[hero?0:2],shoulderFront,elbowFront);
            if(hero)Atlas(texture,new Rect(310,65,325,375),new Rect(-57,-245,114,125));
            else {
                var torso=new Rect(365,258,265,276);var dst=new Rect(-57,-245,114,125);
                AtlasRegion(texture,torso,new Rect(415,258,215,102),dst);
                AtlasRegion(texture,torso,new Rect(380,360,250,174),dst);
            }
            if(hero) {
                // Keep cloth from the pelvis atlas; thighs belong to the moving legs.
                var pelvis=new Rect(650,115,315,350);var dst=new Rect(-57,-145,114,94);
                AtlasRegion(texture,pelvis,new Rect(650,115,315,225),dst);
                for(int strip=0;strip<8;strip++) {
                    float inset=strip*15;
                    AtlasRegion(texture,pelvis,new Rect(650+inset,340+strip*5,315-inset*1.5f,5),dst);
                }
                AtlasRegion(texture,pelvis,new Rect(770,380,135,85),dst);
            } else Atlas(texture,new Rect(655,280,275,270),new Rect(-57,-145,114,80));
            if(hero)Atlas(texture,new Rect(10,74,283,310),new Rect(-32,-304,82,83));
            else {
                var head=new Rect(12,230,390,303);var dst=new Rect(-76,-298,132,103);
                AtlasRegion(texture,head,new Rect(12,230,390,95),dst);
                AtlasRegion(texture,head,new Rect(12,325,330,90),dst);
                AtlasRegion(texture,head,new Rect(12,415,280,118),dst);
            }
            if(hero)Atlas(texture,new Rect(980,145,250,220),new Rect(shoulderFront.x-29,shoulderFront.y-17,60,53));
            if(hero) {
                // The axe belongs to the arm on the character's forward/right side of the screen.
                var handMatrix=GUI.matrix;RotateRig(40+stroke*.85f,handBack);
                var axe=MainTexture("axe");if(axe!=null)Atlas(axe,new Rect(0,0,1280,1280),new Rect(handBack.x-70,handBack.y-147,140,210));GUI.matrix=handMatrix;
                // Grip overlays the shaft at the same socket after drawing the weapon.
                Bone(texture,bones[3],Vector2.LerpUnclamped(elbowBack,handBack,-.10f),handBack,true);
            }
            else {
                // Whole weapon art has a known grip socket; never crop a hand from a character.
                var weapon=MainTexture("axe");var handMatrix=GUI.matrix;
                RotateRig(-55-stroke*.6f,handFront);
                if(weapon!=null)Atlas(weapon,new Rect(0,0,1280,1280),new Rect(handFront.x-33,handFront.y-69,66,99));
                GUI.matrix=handMatrix;
            }
            Bone(texture,bones[hero?1:3],Vector2.LerpUnclamped(elbowFront,handFront,-.10f),handFront,true);
            if(!hero)Atlas(texture,new Rect(970,230,250,290),new Rect(handBack.x-30,handBack.y-33,68,78));
            GUI.matrix=original;captureOnly=previousCaptureOnly;
            if(!captureOnly)DrawComposedPose(captureParts);
            GUI.color=color;
            if(ownsCapture)captureParts=null;
        }
#if UNITY_EDITOR
        public static string ValidateGroundContact()
        {
            float worst=0;int samples=0;
            for(int character=0;character<2;character++)for(int side=0;side<2;side++)for(int mode=0;mode<4;mode++)for(int frame=0;frame<360;frame++) {
                bool hero=character==0,running=mode==1;float cycle=frame*Mathf.Deg2Rad;
                var bones=hero?heroBones:goblinBones;
                var silhouette=hero?(side==0?heroSole0:heroSole1):(side==0?goblinSole0:goblinSole1);
                var hip=new Vector2((side==0?-1:1)*(hero?23:-18),-111);
                float lean=mode==3?-8:mode==2?Mathf.Sin(cycle)*3:running?(hero?4:-7):0;
                float bounce=running?-Mathf.Abs(Mathf.Cos(cycle))*3:Mathf.Sin(cycle)*1.4f;
                var body=Matrix4x4.Translate(new Vector3(0,bounce,0))*Matrix4x4.Translate(new Vector3(0,-110,0))*Matrix4x4.Rotate(Quaternion.Euler(0,0,lean))*Matrix4x4.Translate(new Vector3(0,110,0));
                var sole=StepFoot(hip,cycle,running,hero);
                Vector2 knee,ankle;SolveLeg(hip,sole,bones[side==0?5:7],silhouette,body,hero,running,out knee,out ankle);
                float error=Mathf.Abs(sole.y-SoleY(bones[side==0?5:7],knee,ankle,silhouette,body));
                if(float.IsNaN(error)||float.IsInfinity(error)||error>.1f)throw new System.InvalidOperationException("Foot contact failed: "+character+" / "+side+" / "+mode+" / "+frame+" error="+error);
                worst=Mathf.Max(worst,error);samples++;
            }
            return "Ground contact PASS: "+samples+" poses; max sole error="+worst.ToString("F4")+" reference pixels.";
        }
        public void RenderMotionPreview(float width,float height,float time,int motion)
        {
            if(heroParts==null)heroParts=Resources.Load<Texture2D>("Art/Rigs/hero-parts-v1");
            if(goblinParts==null)goblinParts=Resources.Load<Texture2D>("Art/Rigs/goblin-parts-v1");
            float scale=Mathf.Min(width/1000,height/540);
            var saved=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3(0,75,0),Quaternion.identity,new Vector3(scale,scale,1));
            Box(0,0,1000,540,new Color(.10f,.12f,.15f));Box(0,430,1000,3,new Color(.4f,.4f,.4f));
            if(motion==5) {
                GUI.DrawTexture(new Rect(10,0,500,500),goblinParts);
                foreach(var b in goblinBones)foreach(var v in new[]{b.from,b.to})Box(10+v.x/1280*500,v.y/1280*500,4,4,Color.red);
                GUI.matrix=saved;return;
            }
            float phase=Mathf.Repeat(time,1.5f);
            DrawCutout(true,new Vector2(290,430),1,time,motion==1,motion==2?phase:10,motion==3&&phase<.2f,false);
            if(motion!=4)DrawCutout(false,new Vector2(710,430),.72f,time,motion==1,motion==2?phase:10,motion==3&&phase<.2f,false);
            else {
                captureParts=new System.Collections.Generic.List<RigFragment>(24);captureBase=GUI.matrix.inverse;captureOnly=true;
                DrawCutout(false,new Vector2(710,430),.72f,0,false,10,false,false);
                var fragments=ScatterRig(captureParts,(int)(time/1.5f)*971+17,new Vector2(710,430));captureParts=null;captureOnly=false;
                if(phase<.85f)DrawRigFragments(fragments,phase);
            }
            GUI.matrix=saved;
        }
#endif
    }
}
