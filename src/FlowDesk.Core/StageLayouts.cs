namespace FlowDesk.Core;
public static class StageLayouts {
    public static Rect[] Fit(IReadOnlyList<Rect> bounds,Rect area) {
        if(area.Width<1 || area.Height<1)throw new ArgumentOutOfRangeException(nameof(area));
        if(bounds.Count==0)return [];
        if(bounds.Any(r=>r.Width<1 || r.Height<1))throw new InvalidDataException("Invalid stage bounds.");
        long left=bounds.Min(r=>(long)r.X),top=bounds.Min(r=>(long)r.Y);
        long right=bounds.Max(r=>(long)r.X+r.Width),bottom=bounds.Max(r=>(long)r.Y+r.Height);
        double scale=Math.Min(1d,Math.Min(area.Width/(double)(right-left),area.Height/(double)(bottom-top)));
        double x=area.X+(area.Width-(right-left)*scale)/2d,y=area.Y+(area.Height-(bottom-top)*scale)/2d;
        return bounds.Select(r=>Layouts.Clamp(new((int)Math.Round(x+(r.X-left)*scale),(int)Math.Round(y+(r.Y-top)*scale),Math.Max(1,(int)Math.Round(r.Width*scale)),Math.Max(1,(int)Math.Round(r.Height*scale))),area)).ToArray();
    }
}
