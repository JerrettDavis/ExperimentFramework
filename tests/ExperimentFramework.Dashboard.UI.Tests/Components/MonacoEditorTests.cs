using Bunit;
using ExperimentFramework.Dashboard.UI.Components.Shared;
using ExperimentFramework.Dashboard.UI.Models;

namespace ExperimentFramework.Dashboard.UI.Tests.Components;

public sealed class MonacoEditorTests : BunitContext
{
    [Fact]
    public async Task UserEditWhileInitializationCompletes_IsNotOverwritten()
    {
        JSInterop.SetupVoid("monacoEditor.loadMonaco").SetVoidResult();
        var initialization = JSInterop.SetupVoid("monacoEditor.initialize", _ => true);
        var update = JSInterop.SetupVoid("monacoEditor.setValue", _ => true);
        update.SetVoidResult();
        var markers = JSInterop.SetupVoid("monacoEditor.setMarkers", _ => true);
        markers.SetVoidResult();
        JSInterop.SetupVoid("monacoEditor.dispose", _ => true).SetVoidResult();
        var editor = Render<MonacoEditor>(p => p
            .Add(x => x.Value, "initial")
            .Add(x => x.Markers, [new EditorMarker()]));

        editor.Render(p => p.Add(x => x.Value, "external update"));
        await editor.InvokeAsync(() => editor.Instance.OnContentChanged("user edit"));
        initialization.SetVoidResult();

        editor.WaitForAssertion(() => Assert.Single(markers.Invocations));
        Assert.Empty(update.Invocations);
    }

    [Fact]
    public void ValueChangedWhileEditorIsCreated_IsAppliedAfterInitialization()
    {
        JSInterop.SetupVoid("monacoEditor.loadMonaco").SetVoidResult();
        var initialization = JSInterop.SetupVoid("monacoEditor.initialize", _ => true);
        var update = JSInterop.SetupVoid("monacoEditor.setValue", _ => true);
        update.SetVoidResult();
        JSInterop.SetupVoid("monacoEditor.dispose", _ => true).SetVoidResult();

        var editor = Render<MonacoEditor>(p => p.Add(x => x.Value, ""));
        editor.Render(p => p.Add(x => x.Value, "experiments: []"));
        initialization.SetVoidResult();

        editor.WaitForAssertion(() =>
            Assert.Equal("experiments: []", Assert.Single(update.Invocations).Arguments[1]));
    }

    [Fact]
    public void ValueChangedWhileMonacoLoads_IsUsedWhenEditorIsCreated()
    {
        var loading = JSInterop.SetupVoid("monacoEditor.loadMonaco");
        var initialization = JSInterop.SetupVoid("monacoEditor.initialize", _ => true);
        JSInterop.SetupVoid("monacoEditor.dispose", _ => true).SetVoidResult();

        var editor = Render<MonacoEditor>(p => p.Add(x => x.Value, ""));
        editor.Render(p => p.Add(x => x.Value, "experiments: []"));
        loading.SetVoidResult();

        editor.WaitForAssertion(() =>
        {
            var invocation = Assert.Single(initialization.Invocations);
            var options = invocation.Arguments[1]!;
            Assert.Equal("experiments: []", options.GetType().GetProperty("value")!.GetValue(options));
        });
        initialization.SetVoidResult();
    }
}
