using Terminal.Gui;

namespace Repos.Ui;

public class ModelObjectTreeBuilder : ITreeBuilder<ModelObject>
{
    public bool SupportsCanExpand => true;

    public bool CanExpand(ModelObject model) => model is DirNode { Children.Count: > 0 };

    public IEnumerable<ModelObject> GetChildren(ModelObject model)
    {
        if (model is DirNode a)
            return a.DisplayChildren;

        return [];
    }
}
