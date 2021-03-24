using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Curve;
using Tubular;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MyceliumGenerator : MonoBehaviour
{
    public static float stepLength = 3f;
    public static float randomGrowthFactor = 1.95f;
    public static float maxDistance = 5;
    public static float curveDisplacement = .5f;
    public int maxNodes = 10;
    int maxNeighbours = 2;
    float branchingProbability = .75f;
    List<Mesh> branches = new List<Mesh>();

    public class Node
    {
        public Vector3 position = new Vector3();
        public Vector3 direction;
        public bool tip = true;
        Node parent;

        public Node(Node parent = null)
        {
            this.parent = parent;
            if (parent == null)
            {
                direction = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                );
            }
            else
            {
                direction = parent.direction + GetRandomGrowth(randomGrowthFactor);
                direction.Normalize();
                position = parent.position + (direction * stepLength);
            }
        }

        Vector3 GetRandomGrowth(float growthFactor)
        {
            return new Vector3(
                Random.Range(-.5f, .5f) * growthFactor,
                Random.Range(-.5f, .5f) * growthFactor,
                Random.Range(-.5f, .5f) * growthFactor
            );
        }

        public Mesh MakeConnectionTo(Node other)
        {
            var mid1 = Vector3.Lerp(this.position, other.position, .33f);
            mid1 += RandomCurveDisplacement();
            var mid2 = Vector3.Lerp(this.position, other.position, .66f);
            mid2 += RandomCurveDisplacement();

            var controls = new List<Vector3>() {
                this.position,
                mid1,
                mid2,
                other.position
            };
            var curve = new CatmullRomCurve(controls);
            var mesh = Tubular.Tubular.Build(
                curve: curve,
                tubularSegments: 20,
                radius: .1f,
                radialSegments: 6,
                closed: false
            );
            return mesh;
        }

        Vector3 RandomCurveDisplacement()
        {
            return new Vector3(
                Random.Range(-.5f, .5f) * curveDisplacement,
                Random.Range(-.5f, .5f) * curveDisplacement,
                Random.Range(-.5f, .5f) * curveDisplacement
            );
        }

        public Node[] Beget()
        {
            return new Node[] { new Node(this) };
        }

        public Node[] Branch()
        {
            Node n1 = new Node(this);
            n1.direction += GetRandomGrowth(randomGrowthFactor * 2);
            n1.direction.Normalize();
            Node n2;
            do
            {
                n2 = new Node(this);
            } while (Vector3.Distance(n1.position, n2.position) > maxDistance);
            return new Node[] { n1, n2 };
        }

        public List<Node> GetNeighbours(List<Node> nodes)
        {
            return nodes.FindAll(_ => _.IsCloseTo(this));
        }

        bool IsCloseTo(Node other)
        {
            var d = Vector3.Distance(this.position, other.position);
            return d < maxDistance && d > 0;
        }
    }

    void Start()
    {
        var root = new Node();
        root.position = new Vector3(0, 0, 0);
        root.direction = Vector3.up;
        var nodes = new List<Node>();
        nodes.Add(root);
        int n = 1;
        while (n < maxNodes)
        {
            var tips = nodes.FindAll(_ => _.tip);
            if (tips.Count == 0) break;
            tips.ForEach(parent =>
            {
                var branching = (
                    (parent.GetNeighbours(nodes).Count < maxNeighbours)
                    && (Random.Range(0f, 1f) < branchingProbability)
                );
                var children = branching
                    ? parent.Branch()
                    : parent.Beget();
                foreach (var child in children)
                {
                    nodes.Add(child);
                    branches.Add(child.MakeConnectionTo(parent));
                }
                parent.tip = false;
                n++;
                if (branching) n++;
            });
        }
        var combine = CombineBranches();
        var filter = GetComponent<MeshFilter>();
        filter.mesh = new Mesh();
        filter.mesh.CombineMeshes(combine, true, false);
    }

    CombineInstance[] CombineBranches()
    {
        CombineInstance[] combine = new CombineInstance[branches.Count];
        for (int i = 0; i < branches.Count; i++)
        {
            combine[i].mesh = branches[i];
        }
        return combine;
    }

    void Update()
    {

    }
}
