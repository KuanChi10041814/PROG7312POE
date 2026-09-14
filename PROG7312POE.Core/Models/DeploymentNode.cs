namespace PROG7312POE.Core.Models;

public class DeploymentNode
{
    public string Name { get; set; } = string.Empty;

    public string NodeType { get; set; } = "Zone";

    public List<DeploymentNode> Children { get; set; } = new();

    public bool IsSafelyConfigured { get; set; } = true;

    public static bool ValidateRecursively(DeploymentNode? node)
    {
        if (node is null || string.IsNullOrWhiteSpace(node.Name) || !node.IsSafelyConfigured)
        {
            return false;
        }

        if (node.Children.Count == 0)
        {
            return true;
        }

        return node.Children.All(ValidateRecursively);
    }
}
