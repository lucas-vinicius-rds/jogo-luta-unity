using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class StageTools
{
    static void Neon()
    {
        var plaster = Material("Plaster",new Color(.27f,.29f,.32f),0,.2f,0);
        var pink = Material("Magenta",new Color(.8f,.035f,.26f),.15f,.6f,0,3);
        var warm = Material("InteriorLight",new Color(.9f,.55f,.19f),0,.5f,0,1);
        for(int b=0;b<5;b++)
        {
            float x = (b-2)*5.1f, height = 8+b%2*1.4f;
            Box("ShopBuilding",new Vector3(x,height*.5f,8.5f),new Vector3(4.9f,height,4),plaster);
            Box("ShopRecess",new Vector3(x,1.5f,6.44f),new Vector3(4.4f,2.7f,.15f),glass);
            for(int j=-2;j<=2;j++) Box("ShopMullion",new Vector3(x+j*.85f,1.5f,6.28f),new Vector3(.06f,2.7f,.14f),metal);
            for(int f=0;f<2;f++) for(int w=0;w<3;w++) Window(x+(w-1)*1.4f,4.5f+f*2.1f,6.4f,.9f);
            Sign(new[]{"KITA / RAMEN","ARCADE 08","NORI MARKET","HOTEL K","NIGHT RECORDS"}[b],new Vector3(x,3.15f,6.1f),4.4f,b%2==0?glow:pink);
            Box("ShopAwning",new Vector3(x,2.8f,5.8f),new Vector3(4.8f,.14f,1.1f),metal);
            Box("NeonEdge",new Vector3(x-2.3f,4.6f,6.1f),new Vector3(.09f,4.8f,.1f),b%2==0?pink:glow);
            Pipe(new Vector3(x+2,0,6.2f),new Vector3(x+2,height,6.2f),.06f,metal);
            HVAC(new Vector3(x+1.3f,4.1f,6));
            Box("ShopInterior",new Vector3(x,1.8f,6.2f),new Vector3(3.7f,.08f,.04f),warm);
            for(int tube=0;tube<3;tube++) Box("VerticalNeon",new Vector3(x-2.25f+tube*.12f,5.5f,5.95f),new Vector3(.05f,2.4f,.06f),b%2==0?pink:glow);
            Box("VerticalSignFrame",new Vector3(x+1.9f,5.4f,5.85f),new Vector3(.6f,2.2f,.22f),metal);
            for(int mark=0;mark<4;mark++) { Box("SignGlyphStroke",new Vector3(x+1.9f,4.75f+mark*.42f,5.7f),new Vector3(.36f,.05f,.04f),b%2==0?glow:warm); Box("SignGlyphUpright",new Vector3(x+1.9f,4.75f+mark*.42f,5.7f),new Vector3(.045f,.24f,.04f),b%2==0?glow:warm); }
        }
        for(int i=0;i<12;i++) { float h=9+i%4*2.3f; Box("DistantTower",new Vector3((i-6)*4,h*.5f,20+i%3*4),new Vector3(3.5f,h,4),metal); for(int f=1;f<h;f+=2) for(int w=0;w<2;w++) Box("CityWindow",new Vector3((i-6)*4+w*1.2f-.6f,f,17.95f+i%3*4),new Vector3(.55f,.3f,.03f),glow); }
        for(int i=-6;i<=6;i++) Box("LaneStripe",new Vector3(i*2,.012f,2),new Vector3(1,.015f,.11f),paint,"GameplaySurface");
        for(int i=0;i<5;i++) { float x=(i-2)*4.5f; Box("PavementCurb",new Vector3(x,.1f,4.7f),new Vector3(4.4f,.2f,1.7f),stone); }
        for(int i=0;i<4;i++) { var cover=Box("DrainageGrate",new Vector3(-9+i*6,.015f,3.6f),new Vector3(.9f,.02f,.45f),metal,"GameplaySurface"); for(int slot=0;slot<7;slot++) Box("DrainSlot",cover.transform.position+new Vector3(-.36f+slot*.12f,.015f,0),new Vector3(.035f,.01f,.35f),glass,"GameplaySurface"); }
        Vending(new Vector3(-9,0,4.8f)); Vending(new Vector3(10,0,4.8f));
        for(int wire=0;wire<3;wire++) for(int p=0;p<12;p++) { float x=-14+p*2.5f; float y=8.8f-Mathf.Sin(p/12f*Mathf.PI)*.8f; float y2=8.8f-Mathf.Sin((p+1)/12f*Mathf.PI)*.8f; Pipe(new Vector3(x,y,5+wire),new Vector3(x+2.5f,y2,5+wire),.017f,metal); }
        Point(new Vector3(-7,3,3),new Color(.2f,.65f,1),8,12); Point(new Vector3(7,3,3),new Color(1,.05f,.3f),8,12);
        Weather("Rain",new Vector3(0,10,1),160,42,new Vector3(0,-9,0),new Color(.55f,.65f,.8f,.4f),.045f);
        Weather("StreetSteam",new Vector3(7,.1f,4),24,3,Vector3.up*.5f,new Color(.6f,.65f,.7f,.15f),.7f);
    }
    static void HVAC(Vector3 at)
    {
        Box("AirConditioner",at,new Vector3(1.05f,.65f,.6f),stone);
        for(int j=0;j<8;j++) Box("VentGrille",at+new Vector3(0,-.24f+j*.065f,-.32f),new Vector3(.86f,.025f,.025f),metal);
        Pipe(at+Vector3.right*.48f,at+new Vector3(.48f,-1.2f,.2f),.04f,metal);
    }
    static void Vending(Vector3 at)
    {
        Box("VendingBody",at+Vector3.up, new Vector3(1.1f,2,.7f),metal);
        Box("VendingDisplay",at+new Vector3(-.12f,1.25f,-.38f),new Vector3(.7f,1.15f,.04f),glass);
        for(int row=0;row<3;row++) for(int col=0;col<4;col++) Box("Product",at+new Vector3(-.38f+col*.17f,.85f+row*.3f,-.42f),new Vector3(.1f,.19f,.03f),row%2==0?paint:glow);
        Box("Payment",at+new Vector3(.4f,1.3f,-.4f),new Vector3(.13f,.32f,.05f),glow);
        Box("DeliverySlot",at+new Vector3(0,.35f,-.4f),new Vector3(.7f,.18f,.06f),glass);
    }
    static void Rooftop()
    {
        for(int x=-6;x<=6;x++) for(int z=0;z<6;z++) Box("RoofExpansionJoint",new Vector3(x*2,.008f,z*2-5),new Vector3(.025f,.015f,1.95f),metal,"GameplaySurface");
        Rail(0,5.7f,27); Box("Parapet",new Vector3(0,.28f,6),new Vector3(28,.55f,.4f),stone);
        for(int i=0;i<5;i++) { HVAC(new Vector3(-10+i*1.6f,.8f,4.8f)); Box("PlantSupport",new Vector3(-10+i*1.6f,.28f,4.8f),new Vector3(1.1f,.5f,.7f),metal); }
        Box("ServiceEntrance",new Vector3(9,2.1f,8),new Vector3(4,4.2f,4),stone); Box("RoofDoor",new Vector3(9,1.2f,5.96f),new Vector3(1.25f,2.4f,.08f),metal); Sign("ROOF / 48",new Vector3(9,3,5.85f),2.8f,glow);
        for(int i=0;i<36;i++) { int row=i/12; float x=(i%12-5.5f)*5.2f; float h=5+(i*7%13); float z=22+row*14; Box("Skyline",new Vector3(x,h/2-3,z),new Vector3(3.4f,h,4),i%2==0?glass:stone); for(int f=0;f<h;f++) Box("CurtainWallFloor",new Vector3(x,f-3,z-2.02f),new Vector3(3.45f,.08f,.04f),metal); }
        for(int i=-1;i<=1;i+=2) { Pipe(new Vector3(i*12,0,4),new Vector3(i*12,5,4),.045f,metal); var flag=Box("WindFlag",new Vector3(i*12+1,4.5f,4),new Vector3(2,.7f,.035f),paint,"AmbientAnimation"); flag.AddComponent<StageAmbientMotion>().sway = true; }
        Weather("HighWindDust",new Vector3(0,3,5),12,2,new Vector3(.5f,.05f,0),new Color(.9f,.8f,.65f,.15f),.08f);
    }
    static void Underground()
    {
        var tiles=Material("Ceramic",new Color(.38f,.42f,.4f),.1f,.5f,2);
        Box("StationBackWall",new Vector3(0,3.5f,10),new Vector3(30,7,.7f),tiles);
        for(int i=-3;i<=3;i++) { float x=i*4.2f; Box("RivetedColumn",new Vector3(x,2.8f,6),new Vector3(.65f,5.6f,.65f),metal); Box("ColumnBase",new Vector3(x,.3f,6),new Vector3(1,.6f,1),stone); Box("ColumnCapital",new Vector3(x,5.4f,6),new Vector3(1.2f,.3f,1),metal); for(int h=1;h<5;h++) Box("ColumnRivet",new Vector3(x-.23f,h,5.65f),new Vector3(.08f,.08f,.06f),paint); Box("Fluorescent",new Vector3(x,5.6f,4),new Vector3(2.6f,.12f,.18f),glow); }
        for(int i=-3;i<=3;i++) Arch(new Vector3(i*4.3f,2.2f,8),2.1f,2.9f,metal);
        for(int i=0;i<24;i++) { Box("PlatformWarning",new Vector3(i*1.1f-13,.015f,4.7f),new Vector3(.9f,.025f,.5f),paint,"GameplaySurface"); Box("TrackSleeper",new Vector3(i*1.2f-14,-.25f,12),new Vector3(.3f,.25f,3),wood); }
        Pipe(new Vector3(-18,-.1f,11.1f),new Vector3(18,-.1f,11.1f),.07f,metal); Pipe(new Vector3(-18,-.1f,12.9f),new Vector3(18,-.1f,12.9f),.07f,metal);
        Sign("LINE 04 / LAST TRAIN",new Vector3(0,3.8f,9.55f),6,glow);
        for(int side=-1;side<=1;side+=2) { Box("BenchSeat",new Vector3(side*7,.65f,8),new Vector3(3,.14f,.6f),wood); Box("BenchBack",new Vector3(side*7,1,8.3f),new Vector3(3,.65f,.1f),wood); for(int i=0;i<12;i++) Box("Stair",new Vector3(side*12,i*.2f,8+i*.35f),new Vector3(2,.2f,.4f),stone); }
        Point(new Vector3(0,4,3),new Color(.45f,.8f,.8f),10,15); Weather("StationDust",new Vector3(0,3,6),26,3,new Vector3(.05f,.08f,0),new Color(.6f,.7f,.7f,.2f),.06f);
    }
    static void Arch(Vector3 origin,float radius,float rise,Material material)
    {
        for(int i=0;i<18;i++) { float a=i/18f*Mathf.PI, b=(i+1)/18f*Mathf.PI; var p=origin+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*rise,0); var q=origin+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*rise,0); Pipe(p,q,.12f,material); }
    }
    static void Temple()
    {
        var courtyard=Material("Courtyard",new Color(.38f,.39f,.35f),0,.22f,2);
        for(int x=-9;x<=9;x++) for(int z=-3;z<=3;z++) Box("CourtyardSlab",new Vector3(x*1.4f,.025f,z*1.4f),new Vector3(1.37f,.05f,1.37f),courtyard,"GameplaySurface");
        Box("TempleFoundation",new Vector3(0,.5f,9),new Vector3(18,1,7),stone);
        for(int i=-4;i<=4;i++) { Pipe(new Vector3(i*2,1,6),new Vector3(i*2,5.3f,6),.17f,wood); Box("Capital",new Vector3(i*2,5.2f,6),new Vector3(.8f,.25f,.6f),wood); }
        Box("MainBeam",new Vector3(0,5.4f,7.5f),new Vector3(18,.4f,4),wood);
        for(int i=-7;i<=7;i++) { Box("ShojiUpright",new Vector3(i,3,9.5f),new Vector3(.055f,3.8f,.1f),wood); }
        var paper=Material("ShojiPaper",new Color(.69f,.61f,.42f),0,.1f,0); Box("PaperWall",new Vector3(0,3,9.6f),new Vector3(16,3.7f,.1f),paper);
        for(int y=0;y<6;y++) Box("ShojiLattice",new Vector3(0,1.4f+y*.6f,9.48f),new Vector3(16,.045f,.09f),wood);
        Roof(new Vector3(0,5.5f,8),20,8); Roof(new Vector3(0,7.3f,8),12,5);
        for(int i=0;i<4;i++) Box("TempleStair",new Vector3(0,.125f+i*.25f,4.6f+i*.35f),new Vector3(5,.25f,.4f),stone);
        for(int side=-1;side<=1;side+=2) { Tree(new Vector3(side*12,0,8),6); Tree(new Vector3(side*17,0,14),9); Pipe(new Vector3(side*5,0,4.7f),new Vector3(side*5,2.5f,4.7f),.1f,stone); Box("StoneLantern",new Vector3(side*5,2.2f,4.7f),new Vector3(.65f,.55f,.65f),glow); Roof(new Vector3(side*5,2.5f,4.7f),1.2f,1.2f); }
        Mountains();
        Weather("FallingLeaves",new Vector3(0,7,4),24,4,new Vector3(.5f,-.7f,.1f),new Color(.65f,.38f,.1f,.85f),.09f);
    }
    static void Roof(Vector3 at,float width,float depth)
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int i=0;i<=12;i++) { float x=i/12f*2-1; float y=(1-Mathf.Abs(x))*1.2f+Mathf.Pow(Mathf.Abs(x),5)*.35f; vertices.Add(new Vector3(x*width/2,y,-depth/2)); vertices.Add(new Vector3(x*width/2,y,depth/2)); if(i>0) { int n=i*2; triangles.AddRange(new[]{n-2,n-1,n,n,n-1,n+1,n,n-1,n-2,n+1,n-1,n}); } }
        var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh=Asset(mesh,Folder+"/Art/"+id+"-roof-"+width+".asset");
        var roof=new GameObject("CurvedTileRoof",typeof(MeshFilter),typeof(MeshRenderer)); roof.transform.SetParent(Group("BackgroundArchitecture")); roof.transform.position=at; roof.GetComponent<MeshFilter>().sharedMesh=mesh; roof.GetComponent<Renderer>().sharedMaterial=metal;
        for(int i=0;i<=20;i++) Pipe(at+new Vector3(-width/2+i*width/20,.35f,-depth/2),at+new Vector3(-width/2+i*width/20,.35f,depth/2),.035f,metal);
    }
    static void Tree(Vector3 at,float height)
    {
        Pipe(at,at+Vector3.up*height,.16f,wood);
        var leaves=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Art/"+id+"-CedarNeedles.mat");
        if(leaves==null) { leaves=Material("CedarNeedles",new Color(.1f,.23f,.12f),0,.1f,0); leaves.SetFloat("_Cull",0); EditorUtility.SetDirty(leaves); }
        var leafMesh=LeafSpray();
        for(int level=0;level<5;level++) for(int branch=0;branch<5;branch++) { float angle=branch*Mathf.PI*.4f+level; float radius=(1-level*.14f)*2; var p=at+Vector3.up*(height*.35f+level*height*.12f); var end=p+new Vector3(Mathf.Cos(angle)*radius,.45f,Mathf.Sin(angle)*radius); Pipe(p,end,.045f,wood); for(int twig=0;twig<5;twig++) { var tuft=new GameObject("CedarSpray",typeof(MeshFilter),typeof(MeshRenderer)); tuft.transform.SetParent(Group("MidgroundEnvironment")); tuft.transform.position=Vector3.Lerp(p,end,.2f+twig*.2f); tuft.transform.rotation=Quaternion.Euler(12,angle*Mathf.Rad2Deg+twig*19,15); tuft.GetComponent<MeshFilter>().sharedMesh=leafMesh; tuft.GetComponent<Renderer>().sharedMaterial=leaves; } }
    }
    static Mesh LeafSpray()
    {
        string path=Folder+"/Art/"+id+"-leaf-spray.asset"; var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing!=null) return existing;
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int i=0;i<11;i++) { float angle=(i%2==0?-1:1)*(.4f+i*.035f); var c=new Vector3(0,0,i*.07f); var tip=c+new Vector3(Mathf.Sin(angle)*.35f,.04f,Mathf.Cos(angle)*.3f); var cross=new Vector3(.035f,.015f,0); int n=vertices.Count; vertices.AddRange(new[]{c,cross+(c+tip)*.5f,tip,-cross+(c+tip)*.5f}); triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3}); }
        var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); return Asset(mesh,path);
    }
    static void Mountains()
    {
        var mesh=new Mesh(); var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int i=0;i<15;i++) { float x=(i-7)*8; float h=7+Mathf.PerlinNoise(i*.7f,1)*14; int n=vertices.Count; vertices.AddRange(new[]{new Vector3(x-8,-3,43),new Vector3(x,h,49+i%3*4),new Vector3(x+8,-3,43),new Vector3(x,-3,66)}); triangles.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3,n+3,n+1,n}); }
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh=Asset(mesh,Folder+"/Art/"+id+"-mountain-ridge.asset"); var go=new GameObject("MountainRidge",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(Group("BackgroundArchitecture")); go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<Renderer>().sharedMaterial=Material("MountainSlate",new Color(.21f,.29f,.29f),0,.1f,0);
    }
    static void Ironworks()
    {
        for(int x=-6;x<=6;x++) for(int z=-2;z<=2;z++) { Box("SteelDeckPlate",new Vector3(x*2,.03f,z*2),new Vector3(1.97f,.06f,1.97f),metal,"GameplaySurface"); foreach(float offset in new[]{-.8f,.8f}) Box("DeckBolt",new Vector3(x*2+offset,.07f,z*2+.8f),new Vector3(.07f,.025f,.07f),paint,"GameplaySurface"); }
        for(int i=-2;i<=2;i++) { float x=i*6; Box("GantryColumn",new Vector3(x,5,9),new Vector3(.5f,10,.6f),metal); Box("ColumnFoot",new Vector3(x,.25f,9),new Vector3(1.1f,.5f,1.2f),stone); Pipe(new Vector3(x,3,9),new Vector3(x+5.7f,8,9),.11f,metal); }
        Box("CraneRunway",new Vector3(0,9,9),new Vector3(29,.6f,1.2f),paint); Box("CraneCarriage",new Vector3(2,8.7f,8.3f),new Vector3(3,.8f,2),metal); Pipe(new Vector3(2,8.4f,8),new Vector3(2,5.5f,8),.035f,metal);
        for(int side=-1;side<=1;side+=2)
        {
            var center=new Vector3(side*9,2.5f,13); Pipe(center-Vector3.up*2.5f,center+Vector3.up*2.5f,1.5f,metal);
            for(int j=0;j<4;j++) { var p=center+Vector3.up*(j-1.5f)*1.2f; Pipe(p-Vector3.up*.08f,p+Vector3.up*.08f,1.58f,paint); }
            Pipe(new Vector3(side*9,5,13),new Vector3(side*9,7,13),.25f,metal); Pipe(new Vector3(side*9,7,13),new Vector3(side*3,7,13),.25f,metal);
            Box("FurnaceShell",new Vector3(side*4,2.3f,11),new Vector3(4,4.6f,3),stone); Box("FurnaceMouth",new Vector3(side*4,1.8f,9.45f),new Vector3(2.8f,2.5f,.1f),glow);
            for(int bar=0;bar<8;bar++) Box("FurnaceGrille",new Vector3(side*4-1.3f+bar*.37f,1.8f,9.32f),new Vector3(.07f,2.5f,.14f),metal);
            Point(new Vector3(side*4,2,7),new Color(1,.25f,.04f),14,13);
            Weather("FurnaceSparks",new Vector3(side*4,1.5f,9),30,5,new Vector3(.08f,1.1f,0),new Color(1,.38f,.04f,.9f),.045f);
        }
        Box("Catwalk",new Vector3(0,5.5f,15),new Vector3(26,.2f,2),metal); Rail(0,5.8f,27);
        Sign("IRONWORKS / BAY 07",new Vector3(0,6.8f,10),7,glow);
        Weather("PressureSteam",new Vector3(-10,4,10),35,4,new Vector3(.1f,.6f,0),new Color(.65f,.67f,.7f,.17f),.6f);
    }
    static void Weather(string name,Vector3 position,int maximum,float rate,Vector3 velocity,Color color,float size)
    {
        var particles=new GameObject(name).AddComponent<ParticleSystem>(); particles.transform.SetParent(Group("Atmosphere")); particles.transform.position=position;
        var main=particles.main; main.maxParticles=maximum; main.startLifetime=3; main.startSpeed=0; main.startSize=size; main.startColor=color; main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=particles.emission; emission.rateOverTime=rate;
        var shape=particles.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=name=="Rain"?new Vector3(26,1,10):new Vector3(8,2,3);
        var v=particles.velocityOverLifetime; v.enabled=true; v.space=ParticleSystemSimulationSpace.World; v.x=velocity.x; v.y=velocity.y; v.z=velocity.z;
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false); var pixels=new Color[1024]; for(int y=0;y<32;y++) for(int x=0;x<32;x++) { float radius=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16; pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2)); } texture.SetPixels(pixels); texture.Apply(); texture=Asset(texture,Folder+"/Art/"+id+"-"+name+"-particle.asset");
        var renderer=particles.GetComponent<ParticleSystemRenderer>(); var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); material.SetColor("_BaseColor",Color.white); material.SetTexture("_BaseMap",texture); material.SetFloat("_Surface",1); material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); material.SetFloat("_ZWrite",0); material.renderQueue=3000; material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); renderer.sharedMaterial=Asset(material,Folder+"/Art/"+id+"-"+name+".mat");
        if(name=="Rain") { renderer.renderMode=ParticleSystemRenderMode.Stretch; renderer.lengthScale=5; renderer.velocityScale=.06f; }
    }
}
