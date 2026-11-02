using System;
using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        struct RigFragment
        {
            public Texture2D texture;public Rect source,target;public Matrix4x4 matrix;
            public Vector2 velocity,pivot;public float spin;public Vector4 boneClip;public Vector2 boneRange;
        }
        static RigFragment[] ScatterRig(System.Collections.Generic.List<RigFragment> pose,int seed,Vector2 foot)
        {
            var parts=pose.ToArray();var rng=new System.Random(seed);
            bool burst=rng.Next(3)==0;
            Vector2 slash=(foot-new Vector2(655,620)).normalized;
            float cutY=foot.y-50-(float)rng.NextDouble()*65;
            for(int i=0;i<parts.Length;i++) {
                Vector2 center=parts[i].matrix.MultiplyPoint3x4(parts[i].target.center);
                // Split mode keeps upper/lower groups together; burst mode separates each limb.
                float group=center.y<cutY?-1:1;
                Vector2 scatter=burst?(center-(foot-new Vector2(0,65))).normalized:new Vector2(group*.6f,group);
                float speed=burst?100+(float)rng.NextDouble()*140:85;
                parts[i].velocity=slash*110+scatter*speed+new Vector2(-25,-150);
                parts[i].spin=burst?((float)rng.NextDouble()*2-1)*400:group*95;
                parts[i].pivot=burst?center:new Vector2(foot.x,cutY+group*30);
            }
            return parts;
        }
        readonly System.Collections.Generic.List<RigFragment> deathPose=new System.Collections.Generic.List<RigFragment>(24);
        void DrawRigFragments(RigFragment[] parts,float t)
        {
            var color=GUI.color;deathPose.Clear();
            foreach(var p in parts) {
                Vector3 center=p.pivot;
                Vector2 drift=p.velocity*t+new Vector2(0,300*t*t);
                var moved=p;
                moved.matrix=Matrix4x4.TRS(center+(Vector3)drift,Quaternion.Euler(0,0,p.spin*t),Vector3.one)*Matrix4x4.Translate(-center)*p.matrix;
                deathPose.Add(moved);
            }
            GUI.color=new Color(color.r,color.g,color.b,color.a*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.85f,t))));
            DrawComposedPose(deathPose);GUI.color=color;
        }
        sealed class DeathPiece
        {
            public Rect part;
            public Vector2 velocity;
            public float spin;
        }
        static string MobArt(int sector)
        { return sector==6?"mob-front":sector==5||sector==7?"mob-front-diagonal":sector==2?"mob-back":sector==1||sector==3?"mob-back-diagonal":"mob"; }
        static bool MobFlip(int sector){return sector==4||sector==7||sector==3;}
        static DeathPiece[] MakeDeathPieces(int seed,Vector2 foot)
        {
            // Local random stream: visual variation cannot alter combat or loot RNG.
            var rng=new System.Random(seed);
            bool burst=rng.Next(3)==0;
            int columns=burst?3:1,rows=2;
            bool vertical=!burst&&rng.Next(2)==0;
            float cut=.32f+(float)rng.NextDouble()*.36f;
            var result=new DeathPiece[columns*rows];
            Vector2 slash=(foot-new Vector2(655,620)).normalized;
            if(slash.sqrMagnitude<.01f)slash=Vector2.right;
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++) {
                float top=row==0?0:cut,bottom=row==0?cut:1;
                var part=vertical?new Rect(top,0,bottom-top,1):new Rect(col/(float)columns,top,1f/columns,bottom-top);
                Vector2 scatter=(part.center-new Vector2(.5f,.5f)).normalized;
                float speed=(burst?140:65)+(float)rng.NextDouble()*(burst?130:90);
                result[row*columns+col]=new DeathPiece{part=part,
                    velocity=slash*(70+(float)rng.NextDouble()*90)+scatter*speed+new Vector2(-25,-110-(float)rng.NextDouble()*90),
                    spin=((float)rng.NextDouble()*2-1)*(burst?440:220)};
            }
            return result;
        }
        void DrawDeathFragments(bool front,bool distant=false)
        {
            float now=Time.unscaledTime;
            foreach(var p in impacts) {
                float t=now-p.born;
                if(!p.dead||p.pieces==null||t<0||t>.85f||(p.foot.y>644)!=front)continue;
                if(!front&&p.distant!=distant)continue;
                if(p.rig!=null){DrawRigFragments(p.rig,t);continue;}
                var tex=MainTexture(MobArt(p.sector))??MainTexture("mob");if(tex==null)continue;
                float size=170*Mathf.Lerp(.72f,1.12f,Mathf.InverseLerp(365,840,p.foot.y));
                DrawPieces(tex,new Rect(p.foot.x-size/2,p.foot.y-size*.86f,size,size),p.pieces,t,MobFlip(p.sector));
            }
        }
        static void DrawPieces(Texture2D tex,Rect body,DeathPiece[] pieces,float t,bool flip)
        {
            var matrix=GUI.matrix;var color=GUI.color;
            foreach(var piece in pieces) {
                var r=piece.part;
                Vector2 drift=piece.velocity*t+new Vector2(0,300*t*t);
                var dst=new Rect(body.x+r.x*body.width+drift.x,body.y+r.y*body.height+drift.y,r.width*body.width,r.height*body.height);
                GUI.matrix=matrix;RotateRig(piece.spin*t,dst.center);
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.85f,t));
                GUI.color=new Color(color.r,color.g*(t<.05f?.6f:1),color.b*(t<.05f?.6f:1),color.a*fade);
                GUI.DrawTextureWithTexCoords(dst,tex,new Rect(flip?1-r.x:r.x,1-r.yMax,flip?-r.width:r.width,r.height));
            }
            GUI.color=color;GUI.matrix=matrix;
        }
    }
}
