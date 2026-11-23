using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid navigasi 2D sederhana + A* untuk zombie. Grid dibangun sekali per scene dengan
/// mengetes Collider2D non-trigger (dinding, meja) memakai ukuran collider zombie,
/// jadi jalur yang dihasilkan selalu cukup lebar untuk dilewati.
/// Semua posisi di sini adalah posisi pivot (transform) zombie.
/// </summary>
public sealed class ZombieNav
{
    public const float Cell = 0.15f;

    private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private readonly Vector2 origin;
    private readonly int nx, ny;
    private readonly bool[] walk;

    private readonly float[] g;
    private readonly int[] parent;
    private readonly bool[] closed;
    private readonly List<int> heapNode = new List<int>();
    private readonly List<float> heapPrio = new List<float>();
    private readonly List<int> cells = new List<int>();

    public ZombieNav(Bounds area, Vector2 colliderOffset, Vector2 colliderSize)
    {
        origin = new Vector2(area.min.x, area.min.y);
        nx = Mathf.Max(1, Mathf.CeilToInt(area.size.x / Cell));
        ny = Mathf.Max(1, Mathf.CeilToInt(area.size.y / Cell));
        walk = new bool[nx * ny];
        g = new float[nx * ny];
        parent = new int[nx * ny];
        closed = new bool[nx * ny];

        Physics2D.SyncTransforms();
        Vector2 box = colliderSize * 0.85f;
        for (int y = 0; y < ny; y++)
            for (int x = 0; x < nx; x++)
                walk[y * nx + x] = !IsBlocked(CellCenter(x, y) + colliderOffset, box);
    }

    private static bool IsBlocked(Vector2 center, Vector2 size)
    {
        var hits = Physics2D.OverlapBoxAll(center, size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.isTrigger) continue;
            if (h.GetComponentInParent<PlayerController>() != null) continue;
            if (h.GetComponentInParent<ZombieController>() != null) continue;
            return true;
        }
        return false;
    }

    private Vector2 CellCenter(int x, int y) => origin + new Vector2((x + 0.5f) * Cell, (y + 0.5f) * Cell);

    private Vector2 CellPos(int idx) => CellCenter(idx % nx, idx / nx);

    private int ToIndex(Vector2 p)
    {
        int ix = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / Cell), 0, nx - 1);
        int iy = Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / Cell), 0, ny - 1);
        return iy * nx + ix;
    }

    /// <summary>Cell yang bisa dilewati terdekat dari idx (idx sendiri kalau sudah bisa).</summary>
    private int Nearest(int idx)
    {
        if (walk[idx]) return idx;
        int cx = idx % nx, cy = idx / nx;
        for (int r = 1; r <= 40; r++)
        {
            int best = -1;
            int bestD = int.MaxValue;
            for (int yy = cy - r; yy <= cy + r; yy++)
            {
                if (yy < 0 || yy >= ny) continue;
                for (int xx = cx - r; xx <= cx + r; xx++)
                {
                    if (xx < 0 || xx >= nx) continue;
                    if (Mathf.Max(Mathf.Abs(xx - cx), Mathf.Abs(yy - cy)) != r) continue;
                    if (!walk[yy * nx + xx]) continue;
                    int d = (xx - cx) * (xx - cx) + (yy - cy) * (yy - cy);
                    if (d < bestD) { bestD = d; best = yy * nx + xx; }
                }
            }
            if (best >= 0) return best;
        }
        return -1;
    }

    public Vector2 RandomWalkable()
    {
        for (int i = 0; i < 300; i++)
        {
            int idx = Random.Range(0, walk.Length);
            if (walk[idx]) return CellPos(idx);
        }
        return CellPos(0);
    }

    public bool HasLineOfSight(Vector2 a, Vector2 b)
    {
        float len = Vector2.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(len / (Cell * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            if (!walk[ToIndex(p)]) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ A*

    public bool FindPath(Vector2 from, Vector2 to, List<Vector2> result)
    {
        result.Clear();
        int rawTarget = ToIndex(to);
        int s = Nearest(ToIndex(from));
        int t = Nearest(rawTarget);
        if (s < 0 || t < 0) return false;

        if (s == t)
        {
            result.Add(walk[rawTarget] ? to : CellPos(t));
            return true;
        }

        for (int i = 0; i < g.Length; i++)
        {
            g[i] = float.MaxValue;
            closed[i] = false;
            parent[i] = -1;
        }
        heapNode.Clear();
        heapPrio.Clear();

        g[s] = 0f;
        Push(s, Heuristic(s, t));

        while (heapNode.Count > 0)
        {
            int cur = Pop();
            if (closed[cur]) continue;
            closed[cur] = true;
            if (cur == t) break;

            int cx = cur % nx, cy = cur / nx;
            for (int d = 0; d < 8; d++)
            {
                int x = cx + Dx[d], y = cy + Dy[d];
                if (x < 0 || y < 0 || x >= nx || y >= ny) continue;
                int n = y * nx + x;
                if (!walk[n] || closed[n]) continue;

                bool diagonal = Dx[d] != 0 && Dy[d] != 0;
                if (diagonal && (!walk[cy * nx + x] || !walk[y * nx + cx])) continue; // jangan memotong sudut

                float ng = g[cur] + (diagonal ? 1.4142f : 1f);
                if (ng < g[n])
                {
                    g[n] = ng;
                    parent[n] = cur;
                    Push(n, ng + Heuristic(n, t));
                }
            }
        }

        if (parent[t] < 0) return false;

        cells.Clear();
        for (int c = t; c != -1; c = parent[c]) cells.Add(c);
        cells.Reverse();

        // Haluskan jalur: lompat ke titik terjauh yang masih terlihat lurus.
        int last = cells.Count - 1;
        int idx = 0;
        while (idx < last)
        {
            int j = last;
            while (j > idx + 1 && !HasLineOfSight(CellPos(cells[idx]), CellPos(cells[j]))) j--;
            result.Add(CellPos(cells[j]));
            idx = j;
        }

        if (walk[rawTarget] && result.Count > 0) result[result.Count - 1] = to;
        return true;
    }

    private float Heuristic(int a, int b)
    {
        float dx = Mathf.Abs(a % nx - b % nx);
        float dy = Mathf.Abs(a / nx - b / nx);
        return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
    }

    private void Push(int node, float prio)
    {
        heapNode.Add(node);
        heapPrio.Add(prio);
        int i = heapNode.Count - 1;
        while (i > 0)
        {
            int p = (i - 1) >> 1;
            if (heapPrio[p] <= heapPrio[i]) break;
            Swap(i, p);
            i = p;
        }
    }

    private int Pop()
    {
        int top = heapNode[0];
        int last = heapNode.Count - 1;
        heapNode[0] = heapNode[last];
        heapPrio[0] = heapPrio[last];
        heapNode.RemoveAt(last);
        heapPrio.RemoveAt(last);

        int i = 0, count = heapNode.Count;
        while (true)
        {
            int l = 2 * i + 1, r = l + 1, m = i;
            if (l < count && heapPrio[l] < heapPrio[m]) m = l;
            if (r < count && heapPrio[r] < heapPrio[m]) m = r;
            if (m == i) break;
            Swap(i, m);
            i = m;
        }
        return top;
    }

    private void Swap(int a, int b)
    {
        int n = heapNode[a]; heapNode[a] = heapNode[b]; heapNode[b] = n;
        float p = heapPrio[a]; heapPrio[a] = heapPrio[b]; heapPrio[b] = p;
    }
}
