using ImGuiNET;
using Veldrid;

namespace Repos.Ui
{
    internal static class UiHelper
    {
        public static InputSnapshot Snapshot { get; set; }

        public static bool Button(string label, ModifierKeys modifiers, Key key)
        {
            if (ImGui.Button(label))
                return true;

            foreach (var ke in Snapshot.KeyEvents)
                if (ke.Down && ke.Key == key && ke.Modifiers == modifiers)
                    return true;

            return false;
        }
    }
}
