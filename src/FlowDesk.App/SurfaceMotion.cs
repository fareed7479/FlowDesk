using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using global::Windows.UI.ViewManagement;
namespace FlowDesk.App;
internal static class SurfaceMotion {
    internal const int Feedback=140,EnterDuration=220,StageDuration=320;
    private sealed class State {internal int Generation;internal bool Running,Initialized;}
    private static readonly ConditionalWeakTable<UIElement,State> states=new();
    internal static string? LastError {get;private set;}
    internal static bool ReduceMotion {get;set;}=AppData.LoadReducedMotion();
    internal static bool Allowed {
        get {try{return !ReduceMotion && new UISettings().AnimationsEnabled && !new AccessibilitySettings().HighContrast;}catch{return false;}}
    }
    internal static bool Moving(DependencyObject element) {
        DependencyObject? current=element;
        while(current is not null){if(current is UIElement ui && states.TryGetValue(ui,out var state) && state.Running)return true;current=VisualTreeHelper.GetParent(current);}
        return false;
    }
    internal static void Enter(UIElement element,int milliseconds=EnterDuration) {
        var state=states.GetValue(element,_=>new State());int generation=++state.Generation;
        ElementCompositionPreview.SetIsTranslationEnabled(element,true);state.Initialized=true;
        var visual=ElementCompositionPreview.GetElementVisual(element);
        visual.StopAnimation("Opacity");visual.StopAnimation("Translation");
        visual.Opacity=1;visual.Properties.InsertVector3("Translation",Vector3.Zero);
        if(!Allowed){state.Running=false;return;}
        try {
            LastError=null;state.Running=true;var compositor=visual.Compositor;
            var ease=compositor.CreateCubicBezierEasingFunction(new(.16f,1),new(.3f,1));
            var fade=compositor.CreateScalarKeyFrameAnimation();fade.InsertKeyFrame(0,0);fade.InsertKeyFrame(1,1,ease);fade.Duration=TimeSpan.FromMilliseconds(milliseconds);
            var move=compositor.CreateVector3KeyFrameAnimation();move.InsertKeyFrame(0,new(0,12,0));move.InsertKeyFrame(1,Vector3.Zero,ease);move.Duration=fade.Duration;
            var batch=compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed+=(_,_)=>{if(state.Generation==generation)state.Running=false;batch.Dispose();};
            visual.StartAnimation("Opacity",fade);visual.StartAnimation("Translation",move);batch.End();
        }catch(Exception ex)when(ex is System.Runtime.InteropServices.COMException or ArgumentException or ObjectDisposedException){LastError=ex.Message;state.Running=false;visual.Opacity=1;}
    }
    internal static void Stop(UIElement element) {
        if(!states.TryGetValue(element,out var state))return;
        state.Generation++;state.Running=false;
        try{var visual=ElementCompositionPreview.GetElementVisual(element);visual.StopAnimation("Opacity");if(state.Initialized){visual.StopAnimation("Translation");visual.Properties.InsertVector3("Translation",Vector3.Zero);}visual.Opacity=1;}catch(Exception ex)when(ex is System.Runtime.InteropServices.COMException or ArgumentException or ObjectDisposedException){LastError=ex.Message;}
    }
}