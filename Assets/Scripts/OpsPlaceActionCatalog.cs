using System;

/// <summary>
/// Hierarchical place-targeted actions for the Ops panel (English UI). Leaves may map to <see cref="OperationType"/> when wired.
/// </summary>
public static class OpsPlaceActionCatalog
{
    public sealed class Node
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public OperationType? Operation;
        public Node[] Children = Array.Empty<Node>();
    }

    static Node[] _roots;

    public static Node[] Roots
    {
        get
        {
            if (_roots != null)
                return _roots;
            _roots = new[]
            {
                new Node
                {
                    Id = "recon",
                    Label = "Reconnaissance",
                    Children = new[]
                    {
                        new Node { Id = "scout", Label = "Scout", Operation = OperationType.Scout },
                        new Node { Id = "surveillance", Label = "Surveillance", Operation = OperationType.Surveillance },
                        new Node { Id = "collect", Label = "Collect / pickup", Operation = OperationType.Collect }
                    }
                },
                new Node
                {
                    Id = "attack",
                    Label = "Attack",
                    Children = new[]
                    {
                        new Node { Id = "breaking", Label = "Breaking" },
                        new Node { Id = "arson", Label = "Arson" },
                        new Node { Id = "robbery", Label = "Robbery" },
                        new Node { Id = "vandalism", Label = "Vandalism" },
                        new Node { Id = "burglary", Label = "Burglary" },
                        new Node
                        {
                            Id = "assault_grades",
                            Label = "Assault",
                            Children = new[]
                            {
                                new Node { Id = "assault_light", Label = "Light" },
                                new Node { Id = "assault_heavy", Label = "Heavy" }
                            }
                        }
                    }
                },
                new Node
                {
                    Id = "pressure",
                    Label = "Pressure",
                    Children = new[]
                    {
                        new Node { Id = "shakedown", Label = "Shakedown" },
                        new Node { Id = "intimidation", Label = "Intimidation" }
                    }
                }
            };
            return _roots;
        }
    }
}
