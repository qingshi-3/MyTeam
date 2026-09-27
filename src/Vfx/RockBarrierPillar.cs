using Godot;

namespace TowerAutobattler.Vfx;

// Authored stone geometry is projected onto the same floor axes as the battle.
// Separate textured faces retain real thickness and a visible top when the wall turns.
[GlobalClass]
public partial class RockBarrierPillar : Node2D
{
    [Export] public Polygon2D[] Sides { get; set; } = [];
    [Export] public Polygon2D Crown { get; set; } = null!;
    [Export] public Vector2[] Footprint { get; set; } = [];
    public void Sample(Vector2 tangent,Vector2 forward,float height,float breadth,float thickness,float lift,int variant)
    {
        Visible=lift>.002f;
        if(!Visible)return;
        int count=Footprint.Length;
        var bottom=new Vector2[count];var top=new Vector2[count];var uv=new Vector2[count];
        var texSize=Crown.Texture.GetSize();
        float buried=(1-lift)*height;
        for(int i=0;i<count;i++)
        {
            var p=Footprint[i];
            bottom[i]=tangent*p.X*breadth+forward*p.Y*thickness;
            // A sloped, bevelled crown, not a screen-facing sprite tip.
            float topHeight=Mathf.Max(0,height-(p.X+.5f)*18-(p.Y+.5f)*26-buried);
            top[i]=bottom[i]*.88f+Vector2.Up*topHeight;
            uv[i]=(p*.62f+new Vector2(.5f,.5f))*texSize;
        }
        Crown.Polygon=top;Crown.UV=uv;
        Crown.Color=new Color(1.18f,1.17f,1.15f,1);
        for(int i=0;i<count;i++)
        {
            int next=(i+1)%count;
            var edge=bottom[next]-bottom[i];
            // Positive winding is the side facing the elevated camera.
            var outward=(new Vector2(edge.Y,-edge.X)*Mathf.Sign(tangent.Cross(forward))).Normalized();
            var face=Sides[i];face.Visible=outward.Y>.015f;
            face.Polygon=[bottom[i],bottom[next],top[next],top[i]];
            float left=.04f+Mathf.PosMod(variant*.173f+i*.137f,.50f);
            float right=left+.40f;
            face.UV=[new Vector2(left,.09f+lift*.81f)*texSize,new Vector2(right,.09f+lift*.81f)*texSize,
                new Vector2(right,.09f)*texSize,new Vector2(left,.09f)*texSize];
            float diffuse=.99f-outward.X*.18f;
            face.Color=new Color(diffuse*.96f,diffuse*.985f,diffuse,1);
        }
    }
}
