using Veldrid;

namespace Repos.Ui;

public class Input
{
    public InputSnapshot? Snapshot { get; set; }

    public bool Shift(Key key) => KeyInner(ModifierKeys.Shift, key);
    public bool Ctrl(Key key) => KeyInner(ModifierKeys.Control, key);
    public bool ShiftCtrl(Key key) => KeyInner(ModifierKeys.Shift | ModifierKeys.Control, key);
    public bool Alt(Key key) => KeyInner(ModifierKeys.Alt, key);
    public bool AltCtrl(Key key) => KeyInner(ModifierKeys.Alt | ModifierKeys.Control, key);

    bool KeyInner(ModifierKeys modifiers, Key key)
    {
        if (Snapshot == null)
            return false;

        foreach (var ke in Snapshot.KeyEvents)
            if (ke.Key != Key.ControlLeft)
                if (ke.Down && ke.Key == key && ke.Modifiers == modifiers)
                    return true;

        return false;
    }
}