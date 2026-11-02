using System.Collections.Generic;
using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        sealed class EnemyView { public Vector2 foot; public float hit; public double hp; public readonly List<RigFragment> pose=new List<RigFragment>(16); }
        struct Impact { public Vector2 foot; public float born; public double damage; public bool dead,distant; public int seed,sector; public DeathPiece[] pieces; public RigFragment[] rig; }
        readonly Dictionary<int,EnemyView> enemyViews=new Dictionary<int,EnemyView>();
        readonly List<int> removedViews=new List<int>();
        readonly List<IdleGame.Monster> depthOrder=new List<IdleGame.Monster>();
        readonly List<Impact> impacts=new List<Impact>();
        int seenCombatSeq=-1,visualStage;
        float sweepUntil;
        bool hordeHolding;
        static bool HoldAgainstCrowd(bool holding,int nearby)
        {return holding?nearby>4:nearby>=8;}
        static float MobVariation(int id,int salt)
        {
            uint value=unchecked((uint)id*747796405u+(uint)salt*2891336453u);
            value=((value>>((int)(value>>28)+4))^value)*277803737u;
            return ((value>>22)^value)/4294967295f;
        }
        public void ResetCombatPresentation() {
            enemyViews.Clear();impacts.Clear();seenCombatSeq=-1;visualStage=0;sweepUntil=0;hordeHolding=false;
            heroAttackAt=-10;heroHurtUntil=0;heroWasSwinging=false;rigPreviousHp=-1;
            observedStage=0;observedHp=-1;enemyX=1450;
            coins.Clear();seenKills=-1;seenRevision=-1;seenClear=0;
            deathUntil=rewardUntil=clearUntil=goldPulse=0;
        }
        static readonly Vector2[] entrances={new Vector2(1810,630),new Vector2(1250,960),new Vector2(655,1020),new Vector2(140,960),new Vector2(-180,630),new Vector2(270,530),new Vector2(655,530),new Vector2(1110,530)};
        Vector2 HordeFoot(int id,int sector,float distance)
        {
            sector=((sector%8)+8)%8;
            // Stable per-monster variation spreads a crowd across each entrance without frame jitter.
            float a=sector*Mathf.PI/4+(MobVariation(id,1)-.5f)*.65f;
            float ring=MobVariation(id,2);
            var near=new Vector2(655+Mathf.Cos(a)*(112+ring*100),625+Mathf.Sin(a)*(46+ring*43));
            var entry=entrances[sector]+new Vector2((MobVariation(id,3)-.5f)*170,(MobVariation(id,4)-.5f)*48);
            return Vector2.Lerp(near,entry,Mathf.Clamp01((distance-900)/5100));
        }
        void UpdateHordeVisuals()
        {
            var s=game.Current;
            if(s==null){enemyViews.Clear();impacts.Clear();seenCombatSeq=-1;visualStage=0;hordeHolding=false;return;}
            if(visualStage!=s.state.stage || s.state.bossActive) {enemyViews.Clear();impacts.Clear();seenCombatSeq=s.state.combatSeq;visualStage=s.state.stage;hordeHolding=false;}
            if(s.state.bossActive || s.state.monsters==null){hordeHolding=false;return;}
            int nearby=0;
            foreach(var monster in s.state.monsters)if(monster.x<=2200)nearby++;
            hordeHolding=HoldAgainstCrowd(hordeHolding,nearby);
            var alive=new HashSet<int>();
            foreach(var m in s.state.monsters) {
                alive.Add(m.id);
                var target=HordeFoot(m.id,m.sector,m.x);
                if(!enemyViews.TryGetValue(m.id,out var v)) {
                    // First snapshot stays where the server says; subsequent spawns enter from cover.
                    v=new EnemyView{foot=seenCombatSeq<0?target:entrances[m.sector%8],hp=m.hp};enemyViews.Add(m.id,v);
                }
                v.hp=m.hp;
                v.foot=Vector2.MoveTowards(v.foot,target,Mathf.Max(.001f,Time.unscaledDeltaTime)*680);
            }
            if(seenCombatSeq<0)seenCombatSeq=s.state.combatSeq;
            if(s.state.combatSeq<seenCombatSeq){seenCombatSeq=s.state.combatSeq;impacts.Clear();}
            double firstTick=-1;
            if(s.state.combatEvents!=null)foreach(var e in s.state.combatEvents) {
                if(e.seq<=seenCombatSeq)continue;
                if(firstTick<0)firstTick=e.tick;
                float delay=Mathf.Clamp((float)((e.tick-firstTick)/20),0,1.8f);
                var foot=HordeFoot(e.id,e.sector,e.x);
                if(enemyViews.TryGetValue(e.id,out var v)){v.hit=Time.unscaledTime+.22f;foot=v.foot;}
                int seed=unchecked(e.seq*73856093^e.id*19349663^s.state.stage*83492791);
                if(impacts.Count<40)impacts.Add(new Impact{foot=foot,born=Time.unscaledTime+(e.dead?0:delay),damage=e.damage,dead=e.dead,seed=seed,sector=e.sector,
                    distant=e.sector>=5&&e.x>2800,pieces=e.dead?MakeDeathPieces(seed,foot):null,
                    rig=e.dead&&v!=null&&v.pose.Count>0?ScatterRig(v.pose,seed,foot):null});
                seenCombatSeq=e.seq;
            }
            removedViews.Clear();foreach(var pair in enemyViews)if(!alive.Contains(pair.Key))removedViews.Add(pair.Key);
            foreach(int id in removedViews)enemyViews.Remove(id);
            impacts.RemoveAll(p=>Time.unscaledTime-p.born>1.1f);
        }
        void DrawHorde(bool front=false,bool distant=false)
        {
            var s=game.Current;if(s==null||s.state.monsters==null)return;
            depthOrder.Clear();depthOrder.AddRange(s.state.monsters);
            depthOrder.Sort((a,b)=>{
                float ay=enemyViews.TryGetValue(a.id,out var av)?av.foot.y:0;
                float by=enemyViews.TryGetValue(b.id,out var bv)?bv.foot.y:0;
                int order=ay.CompareTo(by);return order!=0?order:a.id.CompareTo(b.id);
            });
            foreach(var m in depthOrder) {
                if(!enemyViews.TryGetValue(m.id,out var v)||(v.foot.y>644)!=front)continue;
                bool behindRidge=m.sector>=5&&m.x>2800;
                if(!front&&behindRidge!=distant)continue;
                float scale=Mathf.Lerp(.72f,1.12f,Mathf.InverseLerp(365,840,v.foot.y));
                float size=170*scale;
                float bob=game.Connected?Mathf.Sin(Time.unscaledTime*14+m.id)*3:0;
                float recoil=Time.unscaledTime<v.hit?8:0;
                Vector2 push=(v.foot-new Vector2(655,625)).normalized*recoil;
                var rect=new Rect(v.foot.x-size/2+push.x,v.foot.y-size*.86f+bob+push.y,size,size);
                string id=MobArt(m.sector);
                var tex=MainTexture(id)??MainTexture("mob");
                var saved=GUI.color;
                if(Time.unscaledTime<v.hit)GUI.color=new Color(1,.62f,.55f);
                bool flip=MobFlip(m.sector);
                bool rigged=(m.sector==0||m.sector==4)&&DrawGoblinRig(v,size,m.id,m.x,Time.unscaledTime<v.hit,flip);
                if(!rigged&&tex!=null)GUI.DrawTextureWithTexCoords(rect,tex,new Rect(flip?1:0,0,flip?-1:1,1));
                GUI.color=saved;
                if(m.hp<m.maxHp) {
                    Box(v.foot.x-27,rect.y+8,54,5,ink);
                    Box(v.foot.x-27,rect.y+8,54*Mathf.Clamp01((float)(m.hp/m.maxHp)),5,new Color(.85f,.18f,.08f));
                }
            }
        }
        void CombatCover(bool foreground)
        {
            var tex=MainTexture("cover");if(tex==null)return;
            // Scenery uses the same world travel as the map, with seamless mirrored pairs.
            const float width=836;
            float phase=Mathf.Repeat(trackOffset,width*2);
            var saved=GUI.color;
            if(foreground)GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*.78f);
            for(int i=0;i<4;i++) {
                bool flip=(i&1)!=0;
                GUI.DrawTextureWithTexCoords(
                    new Rect(i*width-phase,foreground?650:391,width,230),tex,
                    new Rect(flip?1:0,0,flip?-1:1,foreground?.7f:1));
            }
            GUI.color=saved;
        }
        void DrawCombatImpacts()
        {
            float now=Time.unscaledTime;
            foreach(var p in impacts) {
                float t=now-p.born;
                if(t<0)continue;
                if(t<.24f)sweepUntil=Mathf.Max(sweepUntil,p.born+.24f);
                if(t<.5f)for(int j=0;j<(p.dead?12:5);j++) {
                    float a=(j*2.399f+(p.seed&1023)*.017f)*1.1f;
                    float radius=(p.dead?85:45)*t*2;
                    float x=p.foot.x+Mathf.Cos(a)*radius,y=p.foot.y-60+Mathf.Sin(a)*radius+t*t*90;
                    Box(x,y,j%3==0?7:4,j%2==0?5:3,new Color(.66f,.035f,.035f,1-t*2));
                }
                Text(p.foot.x-50,p.foot.y-125-t*55,100,32,p.damage.ToString("N0"),22,p.dead?gold:ivory,TextAnchor.MiddleCenter);
            }
            if(now<sweepUntil) {
                float phase=1-(sweepUntil-now)/.24f;
                var saved=GUI.matrix;
                // Broad surrounding sweep communicates the radial hit area while the hero faces right.
                for(int i=0;i<16;i++) {
                    float angle=(-150+phase*260-i*5)*Mathf.Deg2Rad;
                    Vector2 point=new Vector2(655+Mathf.Cos(angle)*(Raging?265:210),552+Mathf.Sin(angle)*87);
                    GUIUtility.RotateAroundPivot(angle*Mathf.Rad2Deg+90,point);
                    Box(point.x,point.y,5,24,new Color(1,Raging?.15f:.75f,.2f,(1-i/17f)*(1-phase)));
                    GUI.matrix=saved;
                }
            }
        }
    }
}
